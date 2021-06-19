using System;
using System.Collections.Generic;
using System.Text;

namespace PlanValidation1
{
    /// <summary>
    /// Describes a constant that can be filled in action/rule/task variables. 
    /// </summary>
    class Constant
    {
        public String Name;
        public ConstantType Type;

        public Constant(string v, ConstantType constantType)
        {
            this.Name = v;
            this.Type = constantType;
        }

        /// <summary>
        /// Describes this consatnt in the form of a string. 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            String s = this.Name + " type: " + Type;
            return s;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            Constant cond2 = obj as Constant;
            if (Name.Equals(cond2.Name))
            {
                return this.Type.IsRelated(cond2.Type);
            }
            else return false;
        }

        public override int GetHashCode()
        {
            return (10 * Name.GetHashCode()) + Type.GetHashCode();
        }
    }
}
