using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Pool;

namespace Damdor.Nucleio
{
    /// <summary>
    /// A system resolver that discovers systems dynamically based on attributes.
    /// </summary>
    /// <typeparam name="TSystem">The base type for systems.</typeparam>
    /// <typeparam name="TAttribute">The attribute type used to identify systems.</typeparam>
    public class AttributeBasedSystemResolver<TSystem, TAttribute> : ISystemResolver<TSystem> 
        where TSystem : ISystem
        where TAttribute : Attribute
    {
        private Predicate<Type> predicate;
        private List<Assembly> assemblies;
        private Func<Type, TSystem> create;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeBasedSystemResolver{TSystem, TAttribute}"/> class.
        /// </summary>
        /// <param name="assemblies">The list of assemblies to scan for systems. If null, scans all assemblies in the current AppDomain.</param>
        /// <param name="predicate">An optional predicate to filter systems based on their type.</param>
        /// <param name="create">An optional factory function to create instances of the discovered system types. If null, uses <see cref="Activator.CreateInstance(Type)"/>.</param>
        public AttributeBasedSystemResolver(List<Assembly> assemblies = null, Predicate<Type> predicate = null, Func<Type, TSystem> create = null)
        {
            this.predicate = predicate;
            this.assemblies = assemblies;
            this.create = create;
        }
        
        /// <summary>
        /// Scans the provided assemblies and resolves a list of systems marked with the specified attribute.
        /// </summary>
        /// <returns>A list of discovered and instantiated systems.</returns>
        public List<TSystem> Resolve()
        {
            MakeAssemblyList();
            
            var types = ListPool<Type>.Get();
            foreach (var assembly in assemblies) MakeTypeList(assembly, types);
            var systems = new List<TSystem>(types.Count);
            foreach (var type in types) systems.Add(Create(type));
            ListPool<Type>.Release(types);

            return systems;
        }

        private void MakeAssemblyList()
        {
            assemblies ??= new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
        }
        
        private void MakeTypeList(Assembly assembly, List<Type> types)
        {
            Type[] allTypesInAssembly;

            try
            {
                allTypesInAssembly = assembly.GetTypes();
            }
            catch (Exception)
            {
                // do nothing
                return;
            }

            foreach (var type in allTypesInAssembly)
            {
                if (!typeof(TSystem).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface) continue;
                var attr = type.GetCustomAttribute<TAttribute>();
                if(attr == null) continue;
                if(predicate != null && !predicate(type)) continue;
                types.Add(type);
            }
        }

        private TSystem Create(Type type)
        {
            if (create == null) return (TSystem)Activator.CreateInstance(type);
            return create(type);
        }

    }
}