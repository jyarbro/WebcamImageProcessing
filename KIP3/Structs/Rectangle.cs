namespace KIP3 {
	public struct Rectangle {
		public Rectangle(int ox, int oy, int ex, int ey) {
			Origin = new Point {
				X = ox,
				Y = oy
			};

			Extent = new Point {
				X = ex,
				Y = ey
			};
		}

		public Point Origin { get; set; }
		public Point Extent { get; set; }
	}
}
