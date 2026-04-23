using System;

namespace Damdor.Nucleio
{
    /// <summary>
    /// Attribute used to mark a class as a system.
    /// Can be used in conjunction with <see cref="AttributeBasedSystemResolver{TSystem, TAttribute}"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class NucleioSystemAttribute : Attribute  
    {
    }
}