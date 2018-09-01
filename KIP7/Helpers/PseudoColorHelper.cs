using System;
using System.Runtime.CompilerServices;
using Windows.UI;

namespace KIP7.Helpers {
	/// <summary>
	/// A helper class to manage look-up-table for pseudo-colors.
	/// </summary>
	public static class PseudoColorHelper {
		#region Constructor, private members and methods

		private const int TableSize = 1024;   // Look up table size
		private static readonly uint[] PseudoColorTable;
		private static readonly uint[] InfraredRampTable;

		// Color palette mapping value from 0 to 1 to blue to red colors.
		private static readonly Color[] ColorRamp =
		{
				Color.FromArgb(a:0xFF, r:0x7F, g:0x00, b:0x00),
				Color.FromArgb(a:0xFF, r:0xFF, g:0x00, b:0x00),
				Color.FromArgb(a:0xFF, r:0xFF, g:0x7F, b:0x00),
				Color.FromArgb(a:0xFF, r:0xFF, g:0xFF, b:0x00),
				Color.FromArgb(a:0xFF, r:0x7F, g:0xFF, b:0x7F),
				Color.FromArgb(a:0xFF, r:0x00, g:0xFF, b:0xFF),
				Color.FromArgb(a:0xFF, r:0x00, g:0x7F, b:0xFF),
				Color.FromArgb(a:0xFF, r:0x00, g:0x00, b:0xFF),
				Color.FromArgb(a:0xFF, r:0x00, g:0x00, b:0x7F),
			};

		static PseudoColorHelper() {
			PseudoColorTable = InitializePseudoColorLut();
			InfraredRampTable = InitializeInfraredRampLut();
		}

		/// <summary>
		/// Maps an input infrared value between [0, 1] to corrected value between [0, 1].
		/// </summary>
		/// <param name="value">Input value between [0, 1].</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]  // Tell the compiler to inline this method to improve performance
		private static uint InfraredColor(float value) {
			var index = (int) (value * TableSize);
			index = index < 0 ? 0 : index > TableSize - 1 ? TableSize - 1 : index;
			return InfraredRampTable[index];
		}

		/// <summary>
		/// Initializes the pseudo-color look up table for infrared pixels
		/// </summary>
		private static uint[] InitializeInfraredRampLut() {
			var lut = new uint[TableSize];
			for (var i = 0; i < TableSize; i++) {
				var value = (float) i / TableSize;
				// Adjust to increase color change between lower values in infrared images
				var alpha = (float) Math.Pow(1 - value, 12);
				lut[i] = ColorRampInterpolation(alpha);
			}
			return lut;
		}

		/// <summary>
		/// Initializes pseudo-color look up table for depth pixels
		/// </summary>
		private static uint[] InitializePseudoColorLut() {
			var lut = new uint[TableSize];
			for (var i = 0; i < TableSize; i++) {
				lut[i] = ColorRampInterpolation((float) i / TableSize);
			}
			return lut;
		}

		/// <summary>
		/// Maps a float value to a pseudo-color pixel
		/// </summary>
		private static uint ColorRampInterpolation(float value) {
			// Map value to surrounding indexes on the color ramp
			var rampSteps = ColorRamp.Length - 1;
			var scaled = value * rampSteps;
			var integer = (int) scaled;
			var index =
				integer < 0 ? 0 :
				integer >= rampSteps - 1 ? rampSteps - 1 :
				integer;
			var prev = ColorRamp[index];
			var next = ColorRamp[index + 1];

			// Set color based on ratio of closeness between the surrounding colors
			var alpha = (uint) ((scaled - integer) * 255);
			var beta = 255 - alpha;
			return
				((prev.A * beta + next.A * alpha) / 255) << 24 | // Alpha
				((prev.R * beta + next.R * alpha) / 255) << 16 | // Red
				((prev.G * beta + next.G * alpha) / 255) << 8 |  // Green
				((prev.B * beta + next.B * alpha) / 255);        // Blue
		}

		/// <summary>
		/// Maps a value in [0, 1] to a pseudo RGBA color.
		/// </summary>
		/// <param name="value">Input value between [0, 1].</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint PseudoColor(float value) {
			var index = (int) (value * TableSize);
			index = index < 0 ? 0 : index > TableSize - 1 ? TableSize - 1 : index;
			return PseudoColorTable[index];
		}

		#endregion

		/// <summary>
		/// Maps each pixel in a scanline from a 16 bit depth value to a pseudo-color pixel.
		/// </summary>
		/// <param name="pixelWidth">Width of the input scanline, in pixels.</param>
		/// <param name="inputRowBytes">Pointer to the start of the input scanline.</param>
		/// <param name="outputRowBytes">Pointer to the start of the output scanline.</param>
		/// <param name="depthScale">Physical distance that corresponds to one unit in the input scanline.</param>
		/// <param name="minReliableDepth">Shortest distance at which the sensor can provide reliable measurements.</param>
		/// <param name="maxReliableDepth">Furthest distance at which the sensor can provide reliable measurements.</param>
		public static unsafe void PseudoColorForDepth(int pixelWidth, byte* inputRowBytes, byte* outputRowBytes, float depthScale, float minReliableDepth, float maxReliableDepth) {
			// Visualize space in front of your desktop.
			var minInMeters = minReliableDepth * depthScale;
			var maxInMeters = maxReliableDepth * depthScale;
			var one_min = 1.0f / minInMeters;
			var range = 1.0f / maxInMeters - one_min;

			var inputRow = (ushort*) inputRowBytes;
			var outputRow = (uint*) outputRowBytes;
			for (var x = 0; x < pixelWidth; x++) {
				var depth = inputRow[x] * depthScale;

				if (depth == 0) {
					// Map invalid depth values to transparent pixels.
					// This happens when depth information cannot be calculated, e.g. when objects are too close.
					outputRow[x] = 0;
				}
				else {
					var alpha = (1.0f / depth - one_min) / range;
					outputRow[x] = PseudoColor(alpha * alpha);
				}
			}
		}

		/// <summary>
		/// Maps each pixel in a scanline from a 8 bit infrared value to a pseudo-color pixel.
		/// </summary>
		/// /// <param name="pixelWidth">Width of the input scanline, in pixels.</param>
		/// <param name="inputRowBytes">Pointer to the start of the input scanline.</param>
		/// <param name="outputRowBytes">Pointer to the start of the output scanline.</param>
		public static unsafe void PseudoColorFor8BitInfrared(
			int pixelWidth, byte* inputRowBytes, byte* outputRowBytes) {
			var inputRow = inputRowBytes;
			var outputRow = (uint*) outputRowBytes;
			for (var x = 0; x < pixelWidth; x++) {
				outputRow[x] = InfraredColor(inputRow[x] / (float) byte.MaxValue);
			}
		}

		/// <summary>
		/// Maps each pixel in a scanline from a 16 bit infrared value to a pseudo-color pixel.
		/// </summary>
		/// <param name="pixelWidth">Width of the input scanline.</param>
		/// <param name="inputRowBytes">Pointer to the start of the input scanline.</param>
		/// <param name="outputRowBytes">Pointer to the start of the output scanline.</param>
		public static unsafe void PseudoColorFor16BitInfrared(int pixelWidth, byte* inputRowBytes, byte* outputRowBytes) {
			var inputRow = (ushort*) inputRowBytes;
			var outputRow = (uint*) outputRowBytes;
			for (var x = 0; x < pixelWidth; x++) {
				outputRow[x] = InfraredColor(inputRow[x] / (float) ushort.MaxValue);
			}
		}
	}
}
