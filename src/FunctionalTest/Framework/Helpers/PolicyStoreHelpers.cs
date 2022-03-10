// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Dmf;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;

namespace Microsoft.SqlServer.Test.Manageability.Utils
{
    /// <summary>
    /// Helper methods, constants, etc dealing with the SMO PolicyStore object
    /// </summary>
    public static class PolicyStoreHelpers
    {
        /// <summary>
        /// Creates a local DMF <see cref="Condition"/> object in this <see cref="PolicyStore"/> but does not actually create it on the server
        /// </summary>
        /// <param name="policyStore">The PolicyStore to create the Policy in</param>
        /// <param name="facet">The Facet this Condition is for</param>
        /// <param name="expressionNodeExpression">The expression this Condition will evaluate</param>
        /// <param name="conditionNamePrefix">The prefix for the name of the Policy</param>
        /// <returns></returns>
        public static Condition CreateConditionDefinition(this PolicyStore policyStore, string facet, ExpressionNode expressionNodeExpression, string conditionNamePrefix = "cond_")
        {
            string name = SmoObjectHelpers.GenerateUniqueObjectName(conditionNamePrefix);
           TraceHelper.TraceInformation("Creating Condition definition {0} in PolicyStore {1}", name, policyStore.Name);

            var condition = new Condition(policyStore, name)
            {
                Facet = facet,
                ExpressionNode = expressionNodeExpression
            };

            return condition;
        }

        /// <summary>
        /// Creates a DMF <see cref="Condition"/> object in this <see cref="PolicyStore"/>
        /// </summary>
        /// <param name="policyStore">The PolicyStore to create the Policy in</param>
        /// <param name="facet">The Facet this Condition is for</param>
        /// <param name="expressionNodeExpression">The expression this Condition will evaluate</param>
        /// <param name="conditionNamePrefix">The prefix for the name of the Policy</param>
        /// <returns></returns>
        public static Condition CreateCondition(
            this PolicyStore policyStore, 
            string facet, 
            string expressionNodeExpression, 
            string conditionNamePrefix = "condition_")
        {
            return policyStore.CreateCondition(facet, ExpressionNode.Parse(expressionNodeExpression),
                conditionNamePrefix);
        }

        /// <summary>
        /// Creates a DMF <see cref="Condition"/> object in this <see cref="PolicyStore"/>
        /// </summary>
        /// <param name="policyStore"></param>
        /// <param name="facet"></param>
        /// <param name="expressionNode"></param>
        /// <param name="conditionNamePrefix"></param>
        /// <returns></returns>
        public static Condition CreateCondition(
            this PolicyStore policyStore,
            string facet,
            ExpressionNode expressionNode,
            string conditionNamePrefix = "condition_")
        {
            var condition = policyStore.CreateConditionDefinition(facet, expressionNode, conditionNamePrefix);
           TraceHelper.TraceInformation("Creating Condition {0} in PolicyStore {1}", condition.Name, policyStore.Name);
            condition.Create();
            return condition;
        }
        /// <summary>
        /// Creates a local DMF <see cref="Policy"/> object but does not actually create it on the server
        /// </summary>
        /// <param name="policyStore">The PolicyStore to create the Policy in</param>
        /// <param name="condition">The name of the condition this Policy will evaluate</param>
        /// <param name="policyEvaluationMode">The <see cref="AutomatedPolicyEvaluationMode"/> for the Policy</param>
        /// <param name="objectSet">The name of the object set containing the object conditions this Policy will execute against</param>
        /// <param name="policyNamePrefix">The prefix for the name of the Policy</param>
        /// <returns></returns>
        public static Policy CreatePolicyDefinition(
            this PolicyStore policyStore, 
            string condition, 
            AutomatedPolicyEvaluationMode policyEvaluationMode = AutomatedPolicyEvaluationMode.None,
            string objectSet = null,
            string policyNamePrefix = "policy_")
        {
            string name = SmoObjectHelpers.GenerateUniqueObjectName(policyNamePrefix);
           TraceHelper.TraceInformation("Creating Policy definition {0} in PolicyStore {1}", name, policyStore.Name);

            var policy = new Policy(policyStore, name)
            {
                Condition = condition,
                AutomatedPolicyEvaluationMode = policyEvaluationMode,
                ObjectSet = objectSet
            };

            return policy;
        }

        /// <summary>
        /// Creates a DMF <see cref="Policy"/> object in this <see cref="PolicyStore"/>
        /// </summary>
        /// <param name="policyStore">The PolicyStore to create the Policy in</param>
        /// <param name="condition">The name of the condition this <see cref="Policy"/> will evaluate</param>
        /// <param name="policyEvaluationMode">The <see cref="AutomatedPolicyEvaluationMode"/> for the <see cref="Policy"/></param>
        /// <param name="objectSet">The name of the object set containing the object conditions this <see cref="Policy"/> will execute against</param>
        /// <param name="policyNamePrefix">The prefix for the name of the Policy</param>
        /// <returns></returns>
        public static Policy CreatePolicy(
            this PolicyStore policyStore,
            string condition,
            AutomatedPolicyEvaluationMode policyEvaluationMode = AutomatedPolicyEvaluationMode.None,
            string objectSet = null,
            string policyNamePrefix = "policy_")
        {
            var policy = policyStore.CreatePolicyDefinition(condition, policyEvaluationMode, objectSet, policyNamePrefix);
           TraceHelper.TraceInformation("Creating Policy {0} in PolicyStore {1}", policy.Name, policyStore.Name);
            policy.Create();
            return policy;
        }

        /// <summary>
        /// Creates a local DMF <see cref="ObjectSet"/> object but does not actually create it on the server
        /// </summary>
        /// <param name="policyStore"></param>
        /// <param name="targetSetsAndLevelConditions">A mapping of the set of TargetSets to enable, along with the list of LevelConditions to set
        /// for each TargetSet. The key is the URN Skeleton of the object we're setting LevelConditions for, with the list being a mapping of Levels
        /// along with the Condition name to apply at that level</param>
        /// <param name="facet">The Facet this ObjectSet is for</param>
        /// <param name="objectSetNamePrefix">The prefix for the name of this ObjectSet</param>
        /// <returns></returns>
        public static ObjectSet CreateObjectSetDefinition(
            this PolicyStore policyStore,
            IDictionary<string,IList<Tuple<string,string>>> targetSetsAndLevelConditions, 
            string facet, 
            string objectSetNamePrefix = "object_set_" )
        {
            string name = SmoObjectHelpers.GenerateUniqueObjectName(objectSetNamePrefix);
           TraceHelper.TraceInformation("Creating ObjectSet definition {0} in PolicyStore {1}", name, policyStore.Name);
            var objectSet = new ObjectSet(policyStore, name)
            {
                Facet = facet
            };
            if (targetSetsAndLevelConditions != null)
            {
                foreach (
                    KeyValuePair<string, IList<Tuple<string, string>>> targetSetAndLevelConditions in
                        targetSetsAndLevelConditions)
                {
                    //The key is the target set ID - which is a SFC URN Skeleton of the objects we're creating a set of
                    var targetSet = objectSet.TargetSets[targetSetAndLevelConditions.Key];
                    targetSet.Enabled = true;
                    //Set all the level conditions, which tells the object set what conditions to
                    //apply at each level of the URN
                    foreach (Tuple<string, string> levelCondition in targetSetAndLevelConditions.Value)
                    {
                        targetSet.SetLevelCondition(targetSet.GetLevel(levelCondition.Item1), levelCondition.Item2);
                    }
                }
            }
            return objectSet;
        }

        /// <summary>
        /// Creates a DMF <see cref="ObjectSet"/> object in this <see cref="PolicyStore"/>
        /// </summary>
        /// <param name="policyStore"></param>
        /// <param name="targetSetsAndLevelConditions">A mapping of the set of TargetSets to enable, along with the list of LevelConditions to set
        /// for each TargetSet. The key is the URN Skeleton of the object we're setting LevelConditions for, with the list being a mapping of Levels
        /// along with the Condition name to apply at that level</param>
        /// <param name="facet">The Facet this ObjectSet is for</param>
        /// <param name="objectSetNamePrefix">The prefix for the name of this ObjectSet</param>
        /// <returns></returns>
        public static ObjectSet CreateObjectSet(
            this PolicyStore policyStore,
            IDictionary<string, IList<Tuple<string, string>>> targetSetsAndLevelConditions,
            string facet,
            string objectSetNamePrefix = "object_set_")
        {
            var objectSet = policyStore.CreateObjectSetDefinition(targetSetsAndLevelConditions, facet, objectSetNamePrefix);
           TraceHelper.TraceInformation("Creating ObjectSet {0} in PolicyStore {1}", objectSet.Name, policyStore.Name);
            objectSet.Create();
            return objectSet;
        }
    }
}