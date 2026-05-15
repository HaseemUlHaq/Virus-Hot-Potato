namespace Fusion.Addons.Inspector.Editor
{
	using System;
	using System.Linq.Expressions;
	using System.Reflection;

	internal static class NetworkObjectUtility
	{
		private static readonly Func<NetworkObject, int> _objectInterestGetter = CreateObjectInterestGetter();

		public static int GetObjectInterest(NetworkObject networkObject)
		{
			return _objectInterestGetter(networkObject);
		}

		private static Func<object, int> CreateObjectInterestGetter()
		{
			const string memberName = "ObjectInterest";

			FieldInfo fieldInfo = typeof(NetworkObject).GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (fieldInfo != null)
				return CreateEnumGetter(fieldInfo);

			PropertyInfo propertyInfo = typeof(NetworkObject).GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (propertyInfo != null)
				return CreateEnumGetter(propertyInfo);

			return (obj) => { return default; };
		}

		private static Func<object, int> CreateEnumGetter(FieldInfo fieldInfo)
		{
			ParameterExpression           instanceParam = Expression.Parameter(typeof(object), "instance");
			UnaryExpression               typedInstance = Expression.Convert(instanceParam, fieldInfo.DeclaringType);
			MemberExpression              fieldAccess   = Expression.Field(typedInstance, fieldInfo);
			UnaryExpression               convertToInt  = Expression.Convert(fieldAccess, typeof(int));
			Expression<Func<object, int>> lambda        = Expression.Lambda<Func<object, int>>(convertToInt, instanceParam);

			return lambda.Compile();
		}

		private static Func<object, int> CreateEnumGetter(PropertyInfo propertyInfo)
		{
			ParameterExpression           instanceParam = Expression.Parameter(typeof(object), "instance");
			UnaryExpression               typedInstance = Expression.Convert(instanceParam, propertyInfo.DeclaringType);
			MemberExpression              fieldAccess   = Expression.Property(typedInstance, propertyInfo);
			UnaryExpression               convertToInt  = Expression.Convert(fieldAccess, typeof(int));
			Expression<Func<object, int>> lambda        = Expression.Lambda<Func<object, int>>(convertToInt, instanceParam);

			return lambda.Compile();
		}
	}
}
