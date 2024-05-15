using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaActionGenerators.CandidateGenerators.CPDDLMutexMetaAction
{
    public static class CPDDLParser
    {
        public static List<List<PredicateRule>> ParseCPDDLOutput(string text)
        {
            var rules = new List<List<PredicateRule>>();

            var lines = text.Split(Environment.NewLine);
            foreach (var line in lines)
            {
                if (line.EndsWith(":=1"))
                {
                    var inner = line.Substring(line.IndexOf('{') + 1, line.IndexOf('}') - line.IndexOf('{') - 1);
                    var subRules = new List<PredicateRule>();
                    var subRulesStr = inner.Split(',');
                    bool valid = true;
                    foreach (var subRule in subRulesStr)
                    {
                        var subRuleStr = subRule.Trim();
                        var predName = subRuleStr;
                        if (subRuleStr.Contains(' '))
                            predName = subRuleStr.Substring(0, subRuleStr.IndexOf(' '));
                        if (predName.StartsWith("NOT-"))
                        {
                            valid = false;
                            break;
                        }
                        var fixArgs = new List<string>();
                        if (subRuleStr.Contains(' '))
                        {
                            var args = subRuleStr.Substring(subRuleStr.IndexOf(' ')).Split(' ').ToList();
                            args.RemoveAll(x => x == "");
                            for (int i = 0; i < args.Count; i++)
                            {
                                if (args[i].StartsWith('C'))
                                    fixArgs.Add(args[i].Substring(0, args[i].IndexOf(':')));
                                else if (args[i].StartsWith('V'))
                                    fixArgs.Add(args[i].Substring(0, args[i].IndexOf(':')));
                                else
                                    valid = false;
                            }
                            if (!valid)
                                break;
                        }
                        subRules.Add(new PredicateRule(predName, fixArgs));
                    }

                    if (valid)
                        rules.Add(subRules);
                }
            }

            // Make sure rule argument IDs are unique 
            var index = 0;
            foreach (var ruleSet in rules)
            {
                foreach (var rule in ruleSet)
                    for (int i = 0; i < rule.Args.Count; i++)
                        if (rule.Args[i].StartsWith('C'))
                            rule.Args[i] = $"C{index}_{rule.Args[i].Substring(1)}";
                index++;
            }

            rules = rules.OrderBy(x => x.Count).ToList();

            return rules;
        }
    }
}
