using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Program that solves the plan validation problem.
/// If plan is invalid program suggest which actions to delete to get the longest valid subplan. 
/// Author: Simona Ondrčková
/// </summary>
namespace PlanValidation1
{
    class Program
    {
        static void Main(string[] args)
        {           
                String planS = "plan.txt";
                String domainS = "domain.lisp";
                String problemS = "problem";
                if (args?.Length == 3)
                {
                    domainS = args[0];
                    problemS = args[1];
                    planS = args[2];
                }
                var watch = System.Diagnostics.Stopwatch.StartNew();// measures time   
                List<Action> plan;
                List<TaskType> allTaskTypes = null;
                InputReader reader = new InputReader();
                bool ValidInput=reader.ReadDomain(domainS);
                if (ValidInput)
                {
                    List<ActionType> allActionTypes = reader.GlobalActions;
                    List<ConstantType> allConstantTypes = reader.AllConstantTypes;
                    List<Term> initialState = reader.ReadProblem(problemS, allConstantTypes, ref reader.AllConstants);
                    if (initialState != null)
                    {
                        List<Rule> emptyRules = reader.EmptyRules;
                        List<Constant> allConstants = reader.AllConstants;
                        plan =reader.ReadPlan(planS, allActionTypes, allConstants);
                        allTaskTypes = reader.AlltaskTypes;
                        if (plan!=null)
                        {
                            List<Rule> rules = reader.AllRules;
                            PlanValidator planValidator = new PlanValidator();
                            bool isValid = planValidator.IsPlanValid(plan, allTaskTypes, initialState, allConstants, emptyRules);
                            watch.Stop();
                            var elapsedMs = watch.ElapsedMilliseconds;
                            if (isValid) Console.WriteLine("Plan  is valid. It took {0} s. Plan length {1}", elapsedMs / 1000f, plan.Count);
                            else Console.WriteLine("Plan is invalid. It took {0} s. Plan length {1}", elapsedMs / 1000f, plan.Count);
                        }
                    }
                }
        }
    }
}
