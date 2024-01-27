using System.Reflection;
using Nrrdio.Utilities.Extensions;
using v10.ImageFilters.Contracts;

namespace v10.ImageFilters.Helpers;
public class ImageFilterLoader {
	public static IEnumerable<Type> GetList() {
		var assembly = Assembly.Load("v10.ImageFilters");
		var imageFilterInterface = typeof(IImageFilter);

		return assembly.GetLoadableTypes().Where(t => imageFilterInterface.IsAssignableFrom(t) && t.IsClass);
	}
}
