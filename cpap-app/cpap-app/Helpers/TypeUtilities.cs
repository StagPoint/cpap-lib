using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace cpap_app.Helpers;

public static class TypeUtilities
{
	public static List<T> GetAllPublicConstantValues<T>(this Type type)
	{
		return type
		       .GetTypeInfo()
		       .GetFields( BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy )
		       .Where( fi => fi is { IsLiteral: true, IsInitOnly: false } && fi.FieldType == typeof( T ) )
		       .Select( x => (T)x.GetRawConstantValue()! ?? default( T ) )
		       .ToList()!;
	}
}
