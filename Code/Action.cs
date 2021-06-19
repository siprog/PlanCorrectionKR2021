using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Program that solves the plan validation problem.
/// Author: Simona Ondrčková
/// </summary>
namespace PlanValidation1
{
    /// <summary>
    /// Describes an actual action instance. 
    /// </summary>
    internal class Action
    {
        /// <summary>
        /// Describes the actual action an it's variables. 
        /// </summary>
        public Term ActionInstance { get; }

        /// <summary>
        /// Describes positive precondition with filled variables. 
        /// </summary>
        public List<Term> PosPreConditions { get; }

        /// <summary>
        /// Describes negative precondition with filled variables. 
        /// </summary>
        public List<Term> NegPreConditions { get; } //Checked in validation. 

        /// <summary>
        /// Describes positive effects with filled variables. 
        /// </summary>
        public List<Term> PosEffects { get; }

        /// <summary>
        /// Describes negative effects with filled variables. 
        /// </summary>
        public List<Term> NegEffects { get; }
        public ActionType ActionType;

        /// <summary>
        /// Indexes of actions that must be deleted in order for this task to be valid
        /// </summary>
        public List<int> DeleteActions;

        /// <summary>
        /// For each condition of this task (term) there is an atom timeline and an index of last action that provides support for it but this task does not decompose to it.  
        /// </summary>
        public List<Tuple<Term, List<int>, int>> Supports;

        public Action(Term actionInstance)
        {
            this.ActionInstance = actionInstance;
            this.PosPreConditions = new List<Term>();
            this.NegPreConditions = new List<Term>();
            this.PosEffects = new List<Term>();
            this.NegEffects = new List<Term>();
            DeleteActions = new List<int>();
            Supports = new List<Tuple<Term, List<int>, int>>();
        }

        /// <summary>
        /// Removes conditions from preconditions. 
        /// bool says whether we are removing from positive or negative precondition.
        /// </summary>
        /// <param name="tobeRemoved"></param>
        /// <param name="i"></param>
        public void RemoveConditionsFromPreconditions(List<Term> tobeRemoved, bool i)
        {
            List<Term> myConditions = new List<Term>();
            if (i) myConditions = this.PosPreConditions;
            else myConditions = this.NegPreConditions;
            foreach (Term t in tobeRemoved)
            {
                myConditions.Remove(t);
            }
        }

        /// <summary>
        /// Creates a condition. 
        /// </summary>
        /// <param name="constants">list of all constants</param>
        /// <param name="targetConditions">references to action paramateres for the condition</param>
        /// <param name="FinalConditions">Adds the created condition to this list</param>
        private void CreateCondition(List<Constant> myTypeConstants, List<Constant> allConstants,List<Tuple<string, List<int>>> targetConditions, List<Term> FinalConditions)
        {
            foreach (Tuple<String, List<int>> tuple in targetConditions)
            {
                Constant[] vars = new Constant[tuple.Item2.Count];
                if (tuple.Item1.Contains("!"))
                {
                    //This is a forall condition.We will handle it separately. 
                    List<Term> conditions = CreateForAllCondition(tuple, myTypeConstants, allConstants);
                    FinalConditions.AddRange(conditions);
                }
                else
                {
                    Term condition = FillRest(tuple, myTypeConstants, -1, vars, allConstants);
                    FinalConditions.Add(condition);
                }
            }
        }

        /// <summary>
        /// Creates forall condition. 
        /// </summary>
        /// <param name="tuple"></param>
        /// <param name="myTypeConstants"></param>
        /// <param name="allConstants"></param>
        /// <returns></returns>
        private List<Term> CreateForAllCondition(Tuple<string, List<int>> tuple, List<Constant> myTypeConstants, List<Constant> allConstants)
        {
            String name = tuple.Item1.Replace("!", "");
            List<Term> conditions = new List<Term>();
            for (int i = 0; i < tuple.Item2.Count; i++)
            {
                int j = tuple.Item2[i]; //Reference to either vars or constant list
                if (j <= Globals.ConstReferenceNumber)
                {
                    //First I must simply find the forallacondition. 
                    Constant cExclamation = myTypeConstants[-j + Globals.ConstReferenceNumber];
                    if (cExclamation.Name.Contains("!"))
                    {
                        //I found the forallcondition. 
                        foreach (Constant c in allConstants?.Where(x => cExclamation.Type.IsAncestorTo(x.Type)))
                        {
                            Constant[] vars = new Constant[tuple.Item2.Count];
                            vars[i] = c;
                            Term cond = FillRest(tuple, myTypeConstants, i, vars, allConstants);
                            conditions.Add(cond);
                        }
                        //If there is no constant of given type then both negative and positive forallconditions are valid and so we don't create any conditions for this action as it's essentially always true. 

                    }
                }
            }
            return conditions;
        }

        /// <summary>
        /// Fills all vars for this condition, except for the one with given index. 
        /// This method is used both for creating normal and forall conditions. With forallcondition the index is the index of the forall variable.
        /// For nromal conditions just set index to -1.
        /// </summary>
        /// <param name="tuple"></param>
        /// <param name="myTypeConstants"></param>
        /// <param name="index"></param>
        /// <param name="vars"></param>
        /// <returns></returns>
        private Term FillRest(Tuple<string, List<int>> tuple, List<Constant> myTypeConstants, int index, Constant[] vars, List<Constant> allConstants)
        {
            String name = tuple.Item1.Replace("!", ""); //In case this was forall condition
            for (int i = 0; i < tuple.Item2.Count; i++)
            {
                if (i != index)
                {
                    int j = tuple.Item2[i]; //Reference to either vars or constant list
                    if (j == -2)
                    {
                        //This can only happen in two cases. This condition belongs to an exist condition 
                        //If the j=-2. This means its an exists condition.
                        vars[i] = null;
                    }
                    else if (j <= Globals.ConstReferenceNumber)
                    {
                        //This is a constant 
                        //It cannot be a reference to forall condition, because then i would be equal to index.
                        Constant cExclamation = myTypeConstants[-j + Globals.ConstReferenceNumber];
                        Constant c = allConstants.Find(x => x.Name == cExclamation.Name);
                        if (vars[i] != null) Console.WriteLine("Warning: The parameters of this action's {0} condition {1} are invalid.", this.ActionInstance.Name, name);
                        vars[i] = c;
                    }
                    else
                    {
                        //this is a normal refernce to parameters. 
                        Constant c = allConstants.Find(x => x.Name == ActionInstance.Variables[j].Name); //INFO we do not allow multiple constants with same name but different types.                                                                               
                        if (vars[i] != null) Console.WriteLine("Warning: The parameters of this action's {0} condition {1} are invalid.", this.ActionInstance.Name, name);
                        vars[i] = c;
                    }
                }
            }
            Term condition = new Term(name, vars);
            return condition;
        }

        /// <summary>
        /// Creates conditions for this action.
        /// </summary>
        /// <param name="constants"></param>
        public void CreateConditions(List<Constant> constants)
        {
            if (ActionType != null)
            {
                CreateCondition(ActionType.Constants, constants, ActionType.PosPreConditions, PosPreConditions);
                CreateCondition(ActionType.Constants, constants, ActionType.NegPreConditions, NegPreConditions);
                CreateCondition(ActionType.Constants, constants, ActionType.PosEffects, PosEffects);
                CreateCondition(ActionType.Constants, constants, ActionType.NegEffects, NegEffects);
            }
        }

        /// <summary>
        /// Describes this action in a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string text = string.Join(",", PosPreConditions.Select(x => x.Name));
            string text2 = string.Join(",", PosEffects.Select(x => x.Name));
            string text3 = string.Join(",", NegEffects.Select(x => x.Name));
            string text4 = string.Join(",", NegPreConditions.Select(x => x.Name));
            string vars = string.Join(",", ActionInstance.Variables.Select(x => x.Name));
            String s = "Action: " + this.ActionInstance.Name + " variables " + vars + " preconditions: " + text + " negpreconditions: " + text4 + " posEffects: " + text2 + " negEffects " + text3;
            return s;
        }
    }
}
