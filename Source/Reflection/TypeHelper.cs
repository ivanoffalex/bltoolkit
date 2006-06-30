using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.ComponentModel;
using System.Diagnostics;

using BLToolkit.TypeBuilder;
using BLToolkit.EditableObjects;

namespace BLToolkit.Reflection
{
#if FW2
	[DebuggerDisplay("Type = {Type}")]
#endif
	/// <summary>
	/// A wrapper around the <see cref="Type"/> class.
	/// </summary>
	public class TypeHelper
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TypeHelper"/> class.
		/// </summary>
		/// <param name="type">The Type to wrap.</param>
		public TypeHelper(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			_type = type;
		}

		private Type _type;
		/// <summary>
		/// Gets associated Type.
		/// </summary>
		public  Type  Type
		{
			get { return _type; }
		}

		/// <summary>
		/// Converts the supplied <see cref="Type"/> to a <see cref="TypeHelper"/>.
		/// </summary>
		/// <param name="type">The Type.</param>
		/// <returns>A TypeHelper.</returns>
		public static implicit operator TypeHelper(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return new TypeHelper(type);
		}

		/// <summary>
		/// Converts the supplied <see cref="TypeHelper"/> to a <see cref="Type"/>.
		/// </summary>
		/// <param name="typeHelper">The TypeHelper.</param>
		/// <returns>A Type.</returns>
		public static implicit operator Type(TypeHelper typeHelper)
		{
			if (typeHelper == null) throw new ArgumentNullException("typeHelper");

			return typeHelper.Type;
		}

		#region GetAttributes

		/// <summary>
		/// Returns an array of custom attributes identified by <b>Type</b>.
		/// </summary>
		/// <param name="attributeType">The type of attribute to search for.
		/// Only attributes that are assignable to this type are returned.</param>
		/// <param name="inherit">Specifies whether to search this member's inheritance chain
		/// to find the attributes.</param>
		/// <returns>An array of custom attributes defined on this reflected member,
		/// or an array with zero (0) elements if no attributes are defined.</returns>
		public object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return _type.GetCustomAttributes(attributeType, inherit);
		}

		/// <summary>
		/// Returns an array of custom attributes identified by <b>Type</b>
		/// including attribute's inheritance chain.
		/// </summary>
		/// <param name="attributeType">The type of attribute to search for.
		/// Only attributes that are assignable to this type are returned.</param>
		/// <returns>An array of custom attributes defined on this reflected member,
		/// or an array with zero (0) elements if no attributes are defined.</returns>
		public object[] GetCustomAttributes(Type attributeType)
		{
			return _type.GetCustomAttributes(attributeType, true);
		}


		/// <summary>
		/// Returns an array of all of the custom attributes.
		/// </summary>
		/// <param name="inherit">Specifies whether to search this member's inheritance chain
		/// to find the attributes.</param>
		/// <returns>An array of custom attributes defined on this reflected member,
		/// or an array with zero (0) elements if no attributes are defined.</returns>
		public object[] GetCustomAttributes(bool inherit)
		{
			return _type.GetCustomAttributes(inherit);
		}

		/// <summary>
		/// Returns an array of all of the custom attributes including attributes' inheritance chain.
		/// </summary>
		/// <returns>An array of custom attributes defined on this reflected member,
		/// or an array with zero (0) elements if no attributes are defined.</returns>
		public object[] GetCustomAttributes()
		{
			return _type.GetCustomAttributes(true);
		}

		/// <summary>
		/// Returns an array of all custom attributes identified by <b>Type</b> including type's
		/// inheritance chain.
		/// </summary>
		/// <param name="attributeType">The type of attribute to search for.
		/// Only attributes that are assignable to this type are returned.</param>
		/// <returns>An array of custom attributes defined on this reflected member,
		/// or an array with zero (0) elements if no attributes are defined.</returns>
		public object[] GetAttributes(Type attributeType)
		{
			return GetAttributes(_type, attributeType);
		}

		/// <summary>
		/// Returns an array of all custom attributes including type's inheritance chain.
		/// </summary>
		/// <returns>An array of custom attributes defined on this reflected member,
		/// or an array with zero (0) elements if no attributes are defined.</returns>
		public object[] GetAttributes()
		{
			return GetAttributesInternal();
		}

		private object[] GetAttributesInternal()
		{
			string key = _type.FullName;

			object[] attrs = (object[])_typeAttributes[key];

			if (attrs == null)
			{
				ArrayList list = new ArrayList();

				GetAttributesInternal(list, _type);

				_typeAttributes[key] = attrs = list.ToArray();
			}

			return attrs;
		}

		private static void GetAttributesInternal(ArrayList list, Type type)
		{
			object[] attrs = type.GetCustomAttributes(false);

			foreach (object a in attrs)
				if (list.Contains(a) == false)
					list.Add(a);

			if (type.IsInterface == false)
			{
				// Reflection returns interfaces for the whole inheritance chain.
				// So, we are going to get some hemorrhoid here to restore the inheritance sequence.
				//
				Type[] interfaces      = type.GetInterfaces();
				int    nBaseInterfaces = type.BaseType != null? type.BaseType.GetInterfaces().Length: 0;

				for (int i = 0; i < interfaces.Length; i++)
				{
					Type intf = interfaces[i];

					if (i < nBaseInterfaces)
					{
						bool getAttr = false;

						foreach (MethodInfo mi in type.GetInterfaceMap(intf).TargetMethods)
						{
							// Check if the interface is reimplemented.
							//
							if (mi.DeclaringType == type)
							{
								getAttr = true;
								break;
							}
						}

						if (getAttr == false)
							continue;
					}

					GetAttributesInternal(list, intf);
				}

				if (type.BaseType != null && type.BaseType != typeof(object))
					GetAttributesInternal(list, type.BaseType);
			}
		}

		private static Hashtable _typeAttributes = new Hashtable(10);

		public static object[] GetAttributes(Type type, Type attributeType)
		{
			if (type          == null) throw new ArgumentNullException("type");
			if (attributeType == null) throw new ArgumentNullException("attributeType");

			string key = type.FullName + "." + attributeType.FullName;

			object[] attrs = (object[])_typeAttributes[key];

			if (attrs == null)
			{
				ArrayList list = new ArrayList();

				GetAttributesInternal(list, type);

				for (int i = 0; i < list.Count; i++)
					if (attributeType.IsInstanceOfType(list[i]) == false)
						list.RemoveAt(i--);

				_typeAttributes[key] = attrs = list.ToArray();
			}

			return attrs;
		}

		public static Attribute GetFirstAttribute(Type type, Type attributeType)
		{
			object[] attrs = new TypeHelper(type).GetAttributes(attributeType);

			return attrs.Length > 0? (Attribute)attrs[0]: null;
		}

		#endregion

		#region Property Wrappers

		/// <summary>
		/// Gets the fully qualified name of the Type, including the namespace of the Type.
		/// </summary>
		public string FullName
		{
			get { return _type.FullName; }
		}

		/// <summary>
		/// Gets the name of the Type.
		/// </summary>
		public string Name
		{
			get { return _type.Name; }
		}

		/// <summary>
		/// Gets a value indicating whether the Type is abstract and must be overridden.
		/// </summary>
		public bool IsAbstract
		{
			get { return _type.IsAbstract; }
		}

		/// <summary>
		/// Gets a value indicating whether the System.Type is an array.
		/// </summary>
		public bool IsArray
		{
			get { return _type.IsArray; }
		}

		/// <summary>
		/// Gets a value indicating whether the Type is a value type.
		/// </summary>
		public bool IsValueType
		{
			get { return _type.IsValueType; }
		}

		/// <summary>
		/// Gets a value indicating whether the Type is a class; that is, not a value type or interface.
		/// </summary>
		public bool IsClass
		{
			get { return _type.IsClass; }
		}

		/// <summary>
		/// Indicates whether the Type is serializable.
		/// </summary>
		public bool IsSerializable
		{
			get { return _type.IsSerializable; }
		}

		#endregion

		#region GetMethods

		/// <summary>
		/// Returns all the methods of the current Type.
		/// </summary>
		/// <returns>An array of <see cref="MethodInfo"/> objects representing all methods 
		/// defined for the current Type.</returns>
		public MethodInfo[] GetMethods()
		{
			return _type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		/// <summary>
		/// Returns all the public methods of the current Type.
		/// </summary>
		/// <returns>An array of <see cref="MethodInfo"/> objects representing all the public methods 
		/// defined for the current Type.</returns>
		public MethodInfo[] GetPublicMethods()
		{
			return _type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
		}

		/// <summary>
		/// Searches for the methods defined for the current Type,
		/// using the specified binding constraints.
		/// </summary>
		/// <param name="flags">A bitmask comprised of one or more <see cref="BindingFlags"/> 
		/// that specify how the search is conducted.</param>
		/// <returns>An array of <see cref="MethodInfo"/> objects representing all methods defined 
		/// for the current Type that match the specified binding constraints.</returns>
		public MethodInfo[] GetMethods(BindingFlags flags)
		{
			return _type.GetMethods(flags);
		}

#if FW2

		/// <summary>
		/// Returns all the generic or non-generic methods of the current Type.
		/// </summary>
		/// <param name="generic">True to return all generic methods, false to return all non-generic.</param>
		/// <returns>An array of <see cref="MethodInfo"/> objects representing all methods 
		/// defined for the current Type.</returns>
		public MethodInfo[] GetMethods(bool generic)
		{
			return GetMethods(_type, generic, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		/// <summary>
		/// Returns all the public and non-generic methods of the current Type.
		/// </summary>
		/// <param name="generic">True to return all generic methods, false to return all non-generic.</param>
		/// <returns>An array of <see cref="MethodInfo"/> objects representing all the public methods 
		/// defined for the current Type.</returns>
		public MethodInfo[] GetPublicMethods(bool generic)
		{
			return GetMethods(_type, generic, BindingFlags.Instance | BindingFlags.Public);
		}

		/// <summary>
		/// Searches for the generic methods defined for the current Type,
		/// using the specified binding constraints.
		/// </summary>
		/// <param name="generic">True to return all generic methods, false to return all non-generic.</param>
		/// <param name="flags">A bitmask comprised of one or more <see cref="BindingFlags"/> 
		/// that specify how the search is conducted.</param>
		/// <returns>An array of <see cref="MethodInfo"/> objects representing all methods defined 
		/// for the current Type that match the specified binding constraints.</returns>
		public MethodInfo[] GetMethods(bool generic, BindingFlags flags)
		{
			return GetMethods(_type, generic, flags);
		}

#endif

		#endregion

		#region GetMethod

		public MethodInfo GetMethod(string methodName)
		{
			return _type.GetMethod(methodName,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		public MethodInfo GetPublicMethod(string methodName)
		{
			return _type.GetMethod(methodName,
				BindingFlags.Instance | BindingFlags.Public);
		}

		public MethodInfo GetMethod(string methodName, BindingFlags flags)
		{
			return _type.GetMethod(methodName, flags);
		}

		public MethodInfo GetPublicMethod(string methodName, params Type[] types)
		{
			return _type.GetMethod(
				methodName,
				BindingFlags.Instance | BindingFlags.Public,
				null,
				types,
				null);
		}

		public MethodInfo GetMethod(string methodName, params Type[] types)
		{
			return _type.GetMethod(
				methodName,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null,
				types,
				null);
		}

		public MethodInfo GetMethod(string methodName, BindingFlags flags, params Type[] types)
		{
			return _type.GetMethod(methodName, flags, null, types, null);
		}

#if FW2

		public MethodInfo GetMethod(bool generic, string methodName)
		{
			return GetMethod(_type, generic, methodName,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		public MethodInfo GetPublicMethod(bool generic, string methodName)
		{
			return GetMethod(_type, generic, methodName,
				BindingFlags.Instance | BindingFlags.Public);
		}

		public MethodInfo GetMethod(bool generic, string methodName, BindingFlags flags)
		{
			return GetMethod(_type, generic, methodName, flags);
		}

		public MethodInfo GetPublicMethod(bool generic, string methodName, params Type[] types)
		{
			return _type.GetMethod(methodName,
				BindingFlags.Instance | BindingFlags.Public,
				generic ? GenericBinder.Generic : GenericBinder.NonGeneric,
				types, null);
		}

		public MethodInfo GetMethod(bool generic, string methodName, params Type[] types)
		{
			return _type.GetMethod(methodName,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				generic ? GenericBinder.Generic : GenericBinder.NonGeneric,
				types, null);
		}

		public MethodInfo GetMethod(bool generic, string methodName, BindingFlags flags, params Type[] types)
		{
			return _type.GetMethod(methodName,
				flags,
				generic ? GenericBinder.Generic : GenericBinder.NonGeneric,
				types, null);
		}

#endif
		#endregion

		#region GetFields

		/// <summary>
		/// Returns all the public fields of the current Type.
		/// </summary>
		/// <returns>An array of FieldInfo objects representing all the public fields defined for the current Type.</returns>
		public FieldInfo[] GetFields()
		{
			return _type.GetFields();
		}

		public FieldInfo GetField(string name)
		{
			return _type.GetField(
				name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		#endregion

		#region GetProperties

		/// <summary>
		/// Returns all the public properties of the current Type.
		/// </summary>
		/// <returns>An array of PropertyInfo objects representing all public properties of the current Type.</returns>
		public PropertyInfo[] GetProperties()
		{
			return _type.GetProperties();
		}

		/// <summary>
		/// Searches for the properties of the current Type, using the specified binding constraints.
		/// </summary>
		/// <param name="bindingFlags">A bitmask comprised of one or more <see cref="BindingFlags"/> 
		/// that specify how the search is conducted.</param>
		/// <returns>An array of PropertyInfo objects representing all properties of the current Type
		/// that match the specified binding constraints.</returns>
		public PropertyInfo[] GetProperties(BindingFlags bindingFlags)
		{
			return _type.GetProperties(bindingFlags);
		}

		/// <summary>
		/// Searches for the public property with the specified name.
		/// </summary>
		/// <param name="name">The String containing the name of the public property to get.</param>
		/// <returns>A <see cref="PropertyInfo"/> object representing the public property with the specified name,
		/// if found; otherwise, a null reference.</returns>
		public PropertyInfo GetProperty(string name)
		{
			return _type.GetProperty(
				name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		#endregion

		#region GetInterfaces

		/*
		private Type[] _interfaces;

		/// <summary>
		/// Gets all the interfaces implemented or inherited by the current <see cref="Type"/>.
		/// </summary>
		/// <returns>An array of Type objects representing all the interfaces implemented or
		/// inherited by the current Type,
		/// if found; otherwise, an empty array.</returns>
		public Type[] GetInterfaces()
		{
			if (_interfaces == null)
				_interfaces = _type.GetInterfaces();

			return _interfaces;
		}

		/// <summary>
		/// Gets a specific interface implemented or inherited by the current <see cref="Type"/>.
		/// </summary>
		/// <param name="interfaceType">The type of the interface to get.</param>
		/// <returns>A Type object representing the interface of the specified type, if found;
		///  otherwise, a null reference (Nothing in Visual Basic).</returns>
		public Type GetInterface(Type interfaceType)
		{
			foreach (Type intf in GetInterfaces())
				if (intf == interfaceType)
					return null;

			_type.IsSubclassOf(interfaceType);

			return null;
		}
		*/

		public InterfaceMapping GetInterfaceMap(Type type)
		{
			return _type.GetInterfaceMap(type);
		}

		#endregion

		#region GetConstructor

		/// <summary>
		/// Searches for a public instance constructor whose parameters match the types in the specified array.
		/// </summary>
		/// <param name="types">An array of Type objects representing the number, order, and type of the parameters for the constructor to get.</param>
		/// <returns>A <see cref="ConstructorInfo"/> object representing the public instance constructor whose parameters match the types in the parameter type array, if found; otherwise, a null reference.</returns>
		public ConstructorInfo GetPublicConstructor(params Type[] types)
		{
			return _type.GetConstructor(types);
		}

		public ConstructorInfo GetConstructor(Type type1)
		{
			return GetConstructor(_type, type1);
		}

		public static ConstructorInfo GetConstructor(Type type, params Type[] types)
		{
			if (type == null) throw new ArgumentNullException("type");

			return type.GetConstructor(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null,
				types,
				null);
		}

		/// <summary>
		/// Searches for a public default constructor.
		/// </summary>
		/// <returns>A <see cref="ConstructorInfo"/> object representing the constructor.</returns>
		public ConstructorInfo GetPublicDefaultConstructor()
		{
			return _type.GetConstructor(Type.EmptyTypes);
		}

		/// <summary>
		/// Searches for a default constructor.
		/// </summary>
		/// <returns>A <see cref="ConstructorInfo"/> object representing the constructor.</returns>
		public ConstructorInfo GetDefaultConstructor()
		{
			return GetDefaultConstructor(_type);
		}

		public static ConstructorInfo GetDefaultConstructor(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return type.GetConstructor(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null,
				Type.EmptyTypes,
				null);
		}

		public ConstructorInfo[] GetPublicConstructors()
		{
			return _type.GetConstructors();
		}

		public ConstructorInfo[] GetConstructors()
		{
			return _type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		#endregion

		#region Static Members

		public static Type GetUnderlyingType(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

#if FW2
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
				type = type.GetGenericArguments()[0];
#endif

			if (type.IsEnum)
				type = Enum.GetUnderlyingType(type);

			return type;
		}

		public static bool IsSameOrParent(Type parent, Type child)
		{
			if (parent == null) throw new ArgumentNullException("parent");
			if (child  == null) throw new ArgumentNullException("child");

			if (parent == child ||
				child.IsEnum && Enum.GetUnderlyingType(child) == parent ||
				child.IsSubclassOf(parent))
			{
				return true;
			}

			if (parent.IsInterface)
			{
				Type[] interfaces = child.GetInterfaces();

				foreach (Type t in interfaces)
					if (t == parent)
						return true;
			}

			return false;
		}

#if FW2

		public static MethodInfo GetMethod(Type type, bool generic, string methodName, BindingFlags flags)
		{
			if (type == null) throw new ArgumentNullException("type");

			foreach (MethodInfo method in type.GetMethods(flags))
			{
				if (method.IsGenericMethodDefinition == generic && method.Name == methodName)
					return method;
			}

			return null;
		}

		public static MethodInfo[] GetMethods(Type type, bool generic, BindingFlags flags)
		{
			if (type == null) throw new ArgumentNullException("type");

			return Array.FindAll(
				type.GetMethods(flags),
				delegate(MethodInfo method)
				{
					return method.IsGenericMethodDefinition == generic;
				});
			
		}

#endif

		[SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static object[] GetPropertyParameters(PropertyInfo propertyInfo)
		{
			if (propertyInfo == null) throw new ArgumentNullException("propertyInfo");

			object[] attrs = propertyInfo.GetCustomAttributes(typeof(ParameterAttribute), true);

			if (attrs != null && attrs.Length > 0)
				return ((ParameterAttribute)attrs[0]).Parameters;

			attrs = propertyInfo.GetCustomAttributes(typeof(InstanceTypeAttribute), true);

			if (attrs.Length > 0)
				return ((InstanceTypeAttribute)attrs[0]).Parameters;

			attrs = new TypeHelper(
				propertyInfo.DeclaringType).GetAttributes(typeof(GlobalInstanceTypeAttribute));

			foreach (GlobalInstanceTypeAttribute attr in attrs)
				if (IsSameOrParent(attr.PropertyType, propertyInfo.PropertyType))
//				if (attr.PropertyType == propertyInfo.PropertyType)
					return attr.Parameters;

			return null;
		}

		public static PropertyInfo GetPropertyInfo(
			Type type, string propertyName, Type returnType, Type[] types)
		{
			if (type == null) throw new ArgumentNullException("type");

			return type.GetProperty(
				propertyName,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
				null,
				returnType,
				types,
				null);
		}

		public static Type GetListItemType(object list)
		{
			Type typeOfObject = typeof(object);

			if (list == null)
				return typeOfObject;

			if (list is EditableArrayList)
				return ((EditableArrayList)list).ItemType;

			if (list is Array)
				return list.GetType().GetElementType();

			Type type = list.GetType();

#if FW2
			if (type.IsGenericType)
			{
			}
#endif

			if (list is IList || list is ITypedList || list is IListSource)
			{
				PropertyInfo last = null;

				foreach (PropertyInfo pi in type.GetProperties())
				{
					if (pi.GetIndexParameters().Length > 0 && pi.PropertyType != typeOfObject)
					{
						if (pi.Name == "Item")
							return pi.PropertyType;

						last = pi;
					}
				}

				if (last != null)
					return last.PropertyType;
			}

			if (list is IList)
			{
				IList l = (IList)list;

				for (int i = 0; i < l.Count; i++)
				{
					object o = l[i];

					if (o != null && o.GetType() != typeOfObject)
						return o.GetType();
				}
			}
			else if (list is IEnumerable)
			{
				try
				{
					IEnumerator e = ((IEnumerable)list).GetEnumerator();

					// Yield return does not support Reset.
					//
					try { e.Reset(); } catch {}

					while (e.MoveNext())
					{
						object o = e.Current;

						if (o != null && o.GetType() != typeOfObject)
						{
							try { e.Reset(); } catch {}
							return o.GetType();
						}
					}

					try { e.Reset(); } catch {}
				}
				catch
				{
				}
			}

			return typeOfObject;
		}

		public static Type GetListItemType(Type listType)
		{
			if (IsSameOrParent(typeof(IList),       listType) ||
				IsSameOrParent(typeof(ITypedList),  listType) ||
				IsSameOrParent(typeof(IListSource), listType))
			{
				Type elementType = listType.GetElementType();

				if (elementType != null)
					return elementType;

#if FW2
				if (listType.IsGenericType)
				{
					elementType = listType.GetGenericArguments()[0];

					if (elementType != null)
						return elementType;
				}
#endif

				PropertyInfo last = null;

				foreach (PropertyInfo pi in listType.GetProperties())
				{
					if (pi.GetIndexParameters().Length > 0 && pi.PropertyType != typeof(object))
					{
						if (pi.Name == "Item")
							return pi.PropertyType;

						last = pi;
					}
				}

				if (last != null)
					return last.PropertyType;
			}

			return typeof(object);
		}

		public static bool IsScalar(Type type)
		{
			while (type.IsArray)
				type = type.GetElementType();
			
			return type.IsValueType || type == typeof(string) || type == typeof(Stream);
		}

		public static Type[] GetGenericArguments(Type type, string baseTypeName)
		{
#if FW2
			for (Type t = type; t != typeof(object); t = t.BaseType)
				if (t.IsGenericType && (baseTypeName == null || t.Name.Split('`')[0] == baseTypeName))
					return t.GetGenericArguments();

			foreach (Type t in type.GetInterfaces())
				if (t.IsGenericType && (baseTypeName == null || t.Name.Split('`')[0] == baseTypeName))
					return t.GetGenericArguments();
#endif

			return null;
		}


		#endregion
	}
}