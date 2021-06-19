using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanValidation1
{
    /// <summary>
    /// Describes type of an action. This is used when reading all possible actions, to create references and then to be able to create specific actions.
    /// </summary>
    class ActionType
    {
        public String Name; //Action name
        public int NumOfVariables; // Number of variables. So that load(X,Y) and load(X,Y,Z) are different actions
        public List<Constant> Vars;
        /// <summary>
        /// Some actions have conditions with constants. We can't add the constants to the list of parameters like in methods so we do it here. 
        /// </summary>
        public List<Constant> Constants;
        /// <summary>
        /// List of instances of actions of this type. 
        /// </summary>
        public List<Action> Instances;
        /// <summary>
        /// Positive preconditions in form of links  to parameters. For example for action load(X,Y) and precondition at (X,Y) we have precondition at(0,1).
        /// </summary>
        public List<Tuple<String, List<int>>> PosPreConditions;
        /// <summary>
        /// Negative  preconditions in form of links  to parameters. For example for action load(X,Y) and precondition at (X,Y) we have precondition at(0,1).
        /// </summary>
        public List<Tuple<String, List<int>>> NegPreConditions;
        /// <summary>
        /// Positive effects in form of links  to parameters. For example for action load(X,Y) and effect at (X,Y) we have effect at(0,1).
        /// </summary>
        public List<Tuple<String, List<int>>> PosEffects;
        /// <summary>
        /// Negative effects in form of links  to parameters. For example for action load(X,Y) and effect at (X,Y) we have effect at(0,1).
        /// </summary>
        public List<Tuple<String, List<int>>> NegEffects;

        public ActionType()
        {
            PosPreConditions = new List<Tuple<string, List<int>>>();
            NegPreConditions = new List<Tuple<string, List<int>>>();
            PosEffects = new List<Tuple<string, List<int>>>();
            NegEffects = new List<Tuple<string, List<int>>>();
            Instances = new List<Action>();
        }

        /// <summary>
        /// Describes this instance in form of a string. 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text2 = "";
            if (Instances != null) text2 = string.Join(",", Instances.Select(x => x.ActionInstance.Name));
            String s = "ActionType:" + this.Name + " Num of Variables " + NumOfVariables + " Instances: " + text2;
            s += " PosPrecon ";
            foreach (Tuple<String, List<int>> tuple in PosPreConditions)
            {
                string m = TupleConditionToString(tuple);
                s = s + " " + m;
            }
            s += " NegPrecon ";
            foreach (Tuple<String, List<int>> tuple in NegPreConditions)
            {
                string m = TupleConditionToString(tuple);
                s = s + " " + m;
            }
            s += " PosEffects ";
            foreach (Tuple<String, List<int>> tuple in PosEffects)
            {
                string m = TupleConditionToString(tuple);
                s = s + " " + m;
            }
            s += " NegEffects ";
            foreach (Tuple<String, List<int>> tuple in NegEffects)
            {
                string m = TupleConditionToString(tuple);
                s = s + " " + m;
            }
            return s;
        }

        private String TupleConditionToString(Tuple<String, List<int>> tuple)
        {
            string s = tuple.Item1 + string.Join(",", tuple.Item2);
            return s;
        }
    }
}
