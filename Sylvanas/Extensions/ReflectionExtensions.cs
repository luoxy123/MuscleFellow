using System;
using System.Linq;
using System.Reflection;

namespace Sylvanas.Extensions
{
    public static class ReflectionExtensions
    {
        public static TypeCode GetTypeCode(this Type type)
        {
            return Type.GetTypeCode(type);
        }

        public static TAttr FirstAttribute<TAttr>(this Type type) where TAttr : class
        {
            return type.GetTypeInfo().GetCustomAttributes(typeof(TAttr), true).FirstOrDefault() as TAttr;

        }

        public static bool HasInterface(this Type type, Type interfaceType)
        {
            foreach (var t in type.GetInterfaces())
            {
                if (t == interfaceType)
                {
                    return true;
                }
            }

            return false;
        }
    }
}