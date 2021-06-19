using System;
using System.Collections.Generic;
using System.Text;

namespace PlanValidation1
{
    /// <summary>
    /// Describes constant type. This is important as all actions/rules/tasks have type requirement for their variables. 
    /// Constant of that type or any of its descendants can be filled in. 
    /// </summary>
    class ConstantType
    {
        public String Name;
        /// <summary>
        /// List of all ancestors of this type. 
        /// Must also always contain the task itself. 
        /// Deep -all ancestors
        /// </summary>
        public List<ConstantType> AncestorTypes; 
        /// <summary>
        /// List of all children of this type (not descendants just children.) So it's shallow. 
        /// Never contains itself. 
        /// </summary>
        public List<ConstantType> Children;
        /// <summary>
        /// List of all instances of this type. 
        /// </summary>
        public List<Constant> Instances { get; }
        /// <summary>
        /// List of all descendants of this type. 
        /// Must also always contain the task itself. 
        /// Deep -all descendants
        /// </summary>
        public List<ConstantType> DescendantTypes { get; }

        /// <summary>
        /// Adds new instance to list of instances of this type. 
        /// </summary>
        /// <param name="constant"></param>
        internal void AddInstance(Constant constant)
        {
            if (!Instances.Contains(constant)) this.Instances.Add(constant);
        }

        /// <summary>
        /// Ads c as it's ancestor. Then calls all it's children and they add c as their ancestor too.
        /// Checks if c is not its child, because then we would have a cycle, which we don't allow. 
        /// Does not add itself as a child to the ancestor that must be done separately. 
        /// </summary>
        /// <param name="c"></param>
        public void AddAncestor(ConstantType c)
        {
            if (Children.Contains(c)) Console.WriteLine("Error: Type hierarchy contains cycle regarding tasks {0} and {1}.", this, c);
            else
            {
                if (!AncestorTypes.Contains(c))
                {
                    this.AncestorTypes.Add(c);
                    foreach (ConstantType cT in Children)
                    {
                        cT.AddAncestor(c);
                    }
                }
            }
        }

        /// <summary>
        /// Adds c to its children. Does not call its own ancestor as this is only my child. 
        /// But gives this child link to all my ancestors. 
        /// </summary>
        /// <param name="c"></param>
        public void AddChild(ConstantType c)
        {
            if (AncestorTypes.Contains(c)) Console.WriteLine("Error: Type hierarchy contains cycle regarding tasks {0} and {1}.", this, c);
            else
            {
                Children.Add(c);
                foreach (ConstantType ct in AncestorTypes)
                {
                    c.AddAncestor(ct);
                }
            }
        }

        /// <summary>
        /// Returns true if this type is an ancestor to the given type.
        /// </summary>
        /// <returns></returns>
        public bool IsAncestorTo(ConstantType givenType)
        {
            if (this.Name == "any") return true; //INFO any is child to everything. This is used for constants with unknown types. We dont allow methdos that have unspecified tzpe. As in we cannot have type all (unlless user defined) 
            return givenType.AncestorTypes.Contains(this);
        }

        /// <summary>
        /// Returns true if one of these types is an ancestor to the other. 
        /// </summary>
        /// <param name="givenType"></param>
        /// <returns></returns>
        public bool IsRelated(ConstantType givenType)
        {
            return this.IsAncestorTo(givenType) || givenType.IsAncestorTo(this);
        }

        public ConstantType(String Name)
        {
            this.Name = Name;
            this.AncestorTypes = new List<ConstantType>();
            this.AncestorTypes.Add(this);
            this.Instances = new List<Constant>();
            this.Children = new List<ConstantType>();
            this.DescendantTypes = new List<ConstantType>();
        }

        /// <summary>
        /// Describes this class as a string. 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Name;
        }

        /// <summary>
        /// Tells each of it's ancestors to add this as their descendant. 
        /// </summary>
        internal void CreateDescendantLine()
        {
            foreach (ConstantType c in AncestorTypes)
            {
                c.AddDescendant(this);
            }
        }

        private void AddDescendant(ConstantType c)
        {
            if (!DescendantTypes.Contains(c)) this.DescendantTypes.Add(c);
        }
    }
}
