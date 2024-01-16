using System.Reflection;
using Nrrdio.Utilities.Extensions;
using v9.Core.Contracts;

namespace v9.Core.Helpers;
public class ImageFilterLoader {
	public static IEnumerable<Type> GetList() {
		var assembly = Assembly.Load("v9.Core");
		var imageFilterInterface = typeof(IImageFilter);

		return assembly.GetLoadableTypes().Where(t => imageFilterInterface.IsAssignableFrom(t) && t.IsClass);
	}
}
