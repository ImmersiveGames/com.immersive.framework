using System.Collections.Generic;
using Immersive.Framework.Common;
using UnityEngine;
namespace Immersive.Framework.CycleReset
{
    internal static class ActivityCycleResetTriggerBinding
    {
        internal static ActivityCycleResetTriggerBindingResult TryBind(IReadOnlyList<GameObject> roots, IActivityCycleResetRuntimePort activityCycleResetRuntime)
        {
            int rootCount=CountUniqueRoots(roots);
            if(activityCycleResetRuntime==null) return ActivityCycleResetTriggerBindingResult.Rejected("RejectedMissingActivityCycleResetRuntime",$"Activity Cycle Reset trigger binding requires an Activity Cycle Reset runtime port. roots='{rootCount}' triggers='0' bound='0' idempotent='0' rejected='0'.",rootCount,0,0,0,0);
            List<ActivityCycleResetTrigger> triggers=CollectTriggers(roots);
            if(triggers.Count==0) return ActivityCycleResetTriggerBindingResult.OptionalAbsent(rootCount);
            int bound=0,idempotent=0,rejected=0; var issues=new List<string>();
            for(int i=0;i<triggers.Count;i++) { ActivityCycleResetTrigger trigger=triggers[i]; bool wasBound=trigger.HasActivityCycleResetRuntimeBinding; if(trigger.TryBindActivityCycleResetRuntime(activityCycleResetRuntime,out string issue)){if(wasBound)idempotent++;else bound++;continue;} rejected++; string scene=trigger.gameObject.scene.name.NormalizeTextOrFallback("<unknown>"); issues.Add($"trigger='{trigger.name}' scene='{scene}' issue='{issue.NormalizeTextOrFallback("unknown")}'."); }
            return rejected>0 ? ActivityCycleResetTriggerBindingResult.Rejected("RejectedTriggerBinding",$"Activity Cycle Reset trigger binding failed. roots='{rootCount}' triggers='{triggers.Count}' bound='{bound}' idempotent='{idempotent}' rejected='{rejected}'. {string.Join(" ",issues)}",rootCount,triggers.Count,bound,idempotent,rejected) : ActivityCycleResetTriggerBindingResult.Completed(rootCount,triggers.Count,bound,idempotent);
        }
        private static List<ActivityCycleResetTrigger> CollectTriggers(IReadOnlyList<GameObject> roots) { var result=new List<ActivityCycleResetTrigger>();var seenRoots=new HashSet<GameObject>();var seenTriggers=new HashSet<ActivityCycleResetTrigger>();if(roots==null)return result;for(int i=0;i<roots.Count;i++){GameObject root=roots[i];if(root==null||!seenRoots.Add(root))continue;ActivityCycleResetTrigger[] candidates=root.GetComponentsInChildren<ActivityCycleResetTrigger>(true);for(int j=0;j<candidates.Length;j++)if(candidates[j]!=null&&seenTriggers.Add(candidates[j]))result.Add(candidates[j]);}return result; }
        private static int CountUniqueRoots(IReadOnlyList<GameObject> roots) { var result=new HashSet<GameObject>();if(roots==null)return 0;for(int i=0;i<roots.Count;i++)if(roots[i]!=null)result.Add(roots[i]);return result.Count; }
    }
}
