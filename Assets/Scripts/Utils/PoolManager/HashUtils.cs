using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Utils
{
    public static class HashUtils
    {
        // TODO theoretically a hash can overlap, but unlikely. As we get more object types and players we need to control this edge case with a secondary check.
        const int HASH_PRIME_SEED = 17;
        const int HASH_PRIME = 23;
        const int HASH_IN_PROGRESS = int.MinValue; // Use this instead of 0 for edge cases where a hash computes to 0

        /// <summary>
        /// A collection of types that are ignored by default during processing.
        /// </summary>
        /// <remarks>By default, this collection includes the <see cref="Transform"/> type. Additional
        /// types can be added to this collection if needed.</remarks>
        private static readonly HashSet<Type> IgnoreTypes = new HashSet<Type>
        {
            typeof(Transform)
        };

        // Cache of fields per component type for speed
        private static readonly Dictionary<Type, FieldInfo[]> FieldCache = new Dictionary<Type, FieldInfo[]>();

        /// <summary>
        /// Computes a hash code for the specified <see cref="GameObject"/>.
        /// </summary>
        /// <param name="obj">The <see cref="GameObject"/> for which to compute the hash code. Cannot be <see langword="null"/>.</param>
        /// <returns>An integer representing the hash code of the specified <see cref="GameObject"/>.</returns>
        public static int GetObjectHash(GameObject obj)
        {
            Dictionary<UnityEngine.Object, int> visited = new();
            return HashGameObject(obj, visited);
        }

        /// <summary>
        /// Computes a hash code for the specified <see cref="GameObject"/> and its hierarchy, including its components
        /// and child objects.
        /// </summary>
        /// <remarks>The hash code is computed recursively by combining the hash codes of the <paramref
        /// name="obj"/>'s components and its child objects. The order of child objects and the types of components are
        /// significant in the hash computation. Components of types specified in the <c>IgnoreTypes</c> collection are
        /// excluded from the hash.</remarks>
        /// <param name="obj">The <see cref="GameObject"/> to hash. Must not be <see langword="null"/>.</param>
        /// <param name="visited">A dictionary used to track visited objects during the hashing process to prevent infinite recursion in the
        /// presence of cycles.</param>
        /// <returns>An integer hash code representing the structure and components of the <paramref name="obj"/> and its
        /// hierarchy. Returns -1 if <paramref name="obj"/> is <see langword="null"/>.</returns>
        private static int HashGameObject(GameObject obj, Dictionary<UnityEngine.Object, int> visited)
        {
            if (obj == null) return -1;
            

            // Prevent cycles
            if (visited.TryGetValue(obj, out int cached)) return cached;
            visited[obj] = HASH_IN_PROGRESS;

            unchecked   // Ignores overflows
            {
                int hash = HASH_PRIME_SEED;

                // Hash component structure
                var comps = obj.GetComponents<Component>();
                foreach (var comp in comps)
                {
                    if (comp == null) continue;

                    Type t = comp.GetType();
                    if (IgnoreTypes.Contains(t)) continue;

                    hash = hash * HASH_PRIME + HashComponent(comp, visited);
                }

                // Hash children (order matters)
                int childCount = obj.transform.childCount;
                hash = hash * HASH_PRIME + childCount.GetHashCode();

                for (int i = 0; i < childCount; i++)
                {
                    var child = obj.transform.GetChild(i).gameObject;
                    hash = hash * HASH_PRIME + HashGameObject(child, visited);
                }

                visited[obj] = hash;
                return hash;
            }
        }

        /// <summary>
        /// Computes a hash code for the specified <see cref="Component"/> based on its serialized fields.
        /// </summary>
        /// <remarks>This method calculates the hash code by iterating over the fields of the component
        /// and including only those that are serialized (i.e., public fields not marked with <see
        /// cref="HideInInspector"/> or fields explicitly marked with <see cref="SerializeField"/>). Non-serialized
        /// fields are ignored.</remarks>
        /// <param name="comp">The <see cref="Component"/> to hash. Cannot be <see langword="null"/>.</param>
        /// <param name="visited">A dictionary used to track visited objects during hashing to prevent infinite recursion in cases of circular
        /// references.</param>
        /// <returns>An integer representing the hash code of the component. Returns -1 if the component is <see
        /// langword="null"/>.</returns>
        private static int HashComponent(Component comp, Dictionary<UnityEngine.Object, int> visited)
        {
            if (comp == null) return -1;
            

            Type type = comp.GetType();

            if (!FieldCache.TryGetValue(type, out FieldInfo[] fields))
            {
                fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                FieldCache[type] = fields;
            }

            unchecked
            {
                int hash = HASH_PRIME_SEED;

                foreach (var field in fields)
                {
                    // Only serialize inspector-visible fields
                    bool isSerialized =
                        (field.IsPublic && !field.IsDefined(typeof(HideInInspector), true)) ||
                        field.IsDefined(typeof(SerializeField), true);

                    if (!isSerialized) continue;

                    object value = field.GetValue(comp);
                    hash = hash * HASH_PRIME + HashValue(value, visited);
                }

                return hash;
            }
        }

        /// <summary>
        /// Computes a hash code for the specified value, supporting a wide range of types including primitives, 
        /// strings, enums, Unity objects, collections, and custom objects.
        /// </summary>
        /// <remarks>This method supports hashing for a variety of types, including: <list type="bullet">
        /// <item><description>Primitive types (e.g., <see cref="int"/>, <see cref="float"/>, <see cref="bool"/>,
        /// etc.)</description></item> <item><description>Strings and enums</description></item>
        /// <item><description>Unity objects (<see cref="UnityEngine.Object"/>, including <see
        /// cref="UnityEngine.GameObject"/>  and <see cref="UnityEngine.Component"/>, with structural hashing for
        /// GameObjects and Components)</description></item> <item><description>Collections (e.g., arrays, lists, etc.),
        /// where each element is hashed recursively</description></item> <item><description>Custom objects, by hashing
        /// their fields</description></item> </list> For Unity objects, the method handles <see
        /// cref="UnityEngine.GameObject"/> and <see cref="UnityEngine.Component"/>  structurally, while other Unity
        /// objects are hashed based on their type. Collections are hashed by combining the  hashes of their elements.
        /// Custom objects are hashed by recursively hashing their fields.</remarks>
        /// <param name="value">The object to compute the hash code for. Can be a primitive, string, enum, Unity object,  collection, or
        /// custom object. If <paramref name="value"/> is <see langword="null"/>, the method returns 0.</param>
        /// <param name="visited">A dictionary used to track visited Unity objects during hashing to prevent infinite  recursion in cases of
        /// circular references. This parameter is required when hashing Unity objects.</param>
        /// <returns>An integer representing the hash code of the specified <paramref name="value"/>. The hash code is computed 
        /// based on the type and structure of the object, ensuring consistent results for equivalent values.</returns>
        private static int HashValue(object value, Dictionary<UnityEngine.Object, int> visited)
        {
            if (value == null) return 0;

            unchecked
            {
                if (value is UnityEngine.Object uobj)
                {
                    // Certain unity types can return a fake null that returns true on a type check
                    // True null?
                    if (ReferenceEquals(uobj, null)) return 0;

                    // Unity fake null?
                    if (!uobj) return 0;

                    // Component -> hash owning GameObject if safe
                    if (uobj is Component comp)
                    {
                        // comp.gameObject may still be fake null
                        if (ReferenceEquals(comp.gameObject, null) || !comp.gameObject) return 0;

                        return HashGameObject(comp.gameObject, visited);
                    }

                    // GameObject
                    if (uobj is GameObject go) return HashGameObject(go, visited);

                    // All other Unity objects get type hash only
                    return uobj.GetType().GetHashCode();
                }

                // Strings
                if (value is string s) return s.GetHashCode();

                // Primitive types
                if (value is int i)     return i.GetHashCode();
                if (value is float f)   return f.GetHashCode();
                if (value is bool b)    return b.GetHashCode();
                if (value is double d)  return d.GetHashCode();

                // Enums
                if (value is Enum e)    return e.GetHashCode();

                // Vectors, Colors, Quaternions, etc.
                if (value is Vector2 v2)     return v2.GetHashCode();
                if (value is Vector3 v3)     return v3.GetHashCode();
                if (value is Vector4 v4)     return v4.GetHashCode();
                if (value is Vector2Int v2i) return v2i.GetHashCode();
                if (value is Vector3Int v3i) return v3i.GetHashCode();
                if (value is Color col)      return col.GetHashCode();
                if (value is Quaternion q)   return q.GetHashCode();

                // Arrays or lists
                if (value is System.Collections.IEnumerable enumerable)
                {
                    int hash = HASH_PRIME_SEED;
                    foreach (var item in enumerable)
                        hash = hash * HASH_PRIME + HashValue(item, visited);
                    return hash;
                }

                // Structs and classes - fallback to field hashing
                return HashObjectFields(value, visited);
            }
        }

        /// <summary>
        /// Computes a hash code for the fields of the specified object, including private and public instance fields.
        /// </summary>
        /// <remarks>This method uses reflection to access all instance fields of the object, including
        /// private and non-public fields. It is designed to handle complex object graphs, including those with circular
        /// references, by using the <paramref name="visited"/> dictionary to track already-processed objects.</remarks>
        /// <param name="obj">The object whose fields will be hashed. Must not be <see langword="null"/>.</param>
        /// <param name="visited">A dictionary used to track visited objects during the hashing process to prevent infinite recursion when
        /// hashing objects with circular references. Keys are objects being visited, and values are their hash codes.</param>
        /// <returns>An integer representing the hash code of the object's fields. The hash code is computed recursively for
        /// nested objects and collections.</returns>
        private static int HashObjectFields(object obj, Dictionary<UnityEngine.Object, int> visited)
        {
            Type type = obj.GetType();

            if (!FieldCache.TryGetValue(type, out FieldInfo[] fields))
            {
                fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                FieldCache[type] = fields;
            }

            unchecked
            {
                int hash = HASH_PRIME_SEED;
                foreach (var f in fields)
                {
                    object val = f.GetValue(obj);
                    hash = hash * HASH_PRIME + HashValue(val, visited);
                }
                return hash;
            }
        }
    }
}
