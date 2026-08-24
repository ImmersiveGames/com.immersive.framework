using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>Session owner of the Player camera capability, including request publication and retryable release.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "P3K.4 Player camera capability authority.")]
    internal sealed class PlayerGameplayCameraEligibilityRuntimeContext
    {
        private sealed class Record { internal LocalPlayerCameraRequestPublisher publisher; internal CameraRequest request; internal PlayerGameplayCameraAuthoring authoring; }
        private readonly string _sessionContextId; private readonly PlayerSlotId[] _orderedSlots;
        private readonly Dictionary<PlayerSlotId, PlayerGameplayCameraEligibilitySummary> _slots = new();
        private readonly Dictionary<PlayerSlotId, Record> _records = new();
        private int _revision = 1; private int _sequence;
        private PlayerGameplayCameraEligibilityStatus _lastOperationStatus; private string _lastOperationMessage = "Player camera capability runtime initialized.";
        private PlayerGameplayCameraEligibilityRuntimeContext(string session, PlayerSlotId[] slots) { _sessionContextId=session; _orderedSlots=slots; foreach(var slot in slots) this._slots.Add(slot, PlayerGameplayCameraEligibilitySummary.NotEvaluated(session,slot,0,nameof(PlayerGameplayCameraEligibilityRuntimeContext),"runtime-initialization","Configured Player Slot has no camera capability.")); }
        internal string SessionContextId => _sessionContextId; internal int Revision => _revision;
        internal static bool TryCreate(PlayerGameplayOccupancyRuntimeContext occupancy, PlayerGameplayInputBindingRuntimeContext input, out PlayerGameplayCameraEligibilityRuntimeContext context, out string issue)
        {
            context=null; issue=string.Empty; var snapshot=occupancy?.CreateSnapshot();
            if(snapshot == null || !snapshot.IsInitialized || snapshot.ConfiguredSlotCount == 0) { issue="Camera capability requires initialized occupancy Slot roster."; return false; }
            var ordered=new PlayerSlotId[snapshot.ConfiguredSlotCount]; for(int i=0;i<ordered.Length;i++) ordered[i]=snapshot.Slots[i].PlayerSlotId;
            context=new PlayerGameplayCameraEligibilityRuntimeContext(snapshot.SessionContextId,ordered); return true;
        }
        internal PlayerGameplayCameraEligibilityResult TryConfirmEligibility(PlayerActorPreparationSummary preparation, PlayerGameplayOccupancySummary ignoredOccupancy, PlayerGameplayInputBindingSummary ignoredInput, CameraOutputSessionBinding outputSession, PlayerActorDeclaration actor, PlayerGameplayCameraAuthoring authoring, string source, string reason)
        {
            const string op="ConfirmCameraEligibility"; var slot=preparation.PlayerSlotId; var previous=Get(slot);
            if(!ValidatePreparation(preparation, slot, out var issue) || authoring==null || actor==null || authoring.CameraRig==null || authoring.FollowTarget==null) return Reject(PlayerGameplayCameraEligibilityStatus.RejectedInvalidRequest,op,slot,previous,string.IsNullOrEmpty(issue)?"Camera capability requires Actor-owned authoring, rig and Follow target.":issue);
            if(!TryGetOutput(outputSession,out var output,out issue)) return Reject(PlayerGameplayCameraEligibilityStatus.RejectedInvalidRequest,op,slot,previous,issue);
            if(previous.HasCurrentDecision) {
                if(previous.PreparationToken==preparation.Token && previous.Token.CameraOutputId==output.OutputId.ToString() && previous.IsEligible && _records.TryGetValue(slot,out var current) && ReferenceEquals(current.authoring,authoring)) return Result(PlayerGameplayCameraEligibilityStatus.SucceededAlreadyEligible,op,slot,previous,previous,"Camera capability is already published on the expected output.");
                return Reject(PlayerGameplayCameraEligibilityStatus.RejectedSlotAlreadyEvaluated,op,slot,previous,"Player Slot already has another current camera capability.");
            }
            var token=new PlayerGameplayCameraEligibilityToken(_sessionContextId,slot,preparation.Token,output.OutputId.ToString(),++_sequence);
            string requestId=$"player.camera:{_sessionContextId}:{slot.Value.Value}:{token.CameraRevision}";
            var created=CameraRequestCreateResult.Create(new CameraRequestId(requestId),output.OutputId,new CameraRequestOwner(CameraRequestOwnerKind.LocalPlayer,slot.Value.Value),new CameraRequestLifetime(CameraRequestLifetimeKind.LocalPlayerEligibility,$"player.camera:{_sessionContextId}:{slot.Value.Value}:{token.CameraRevision}"),CameraRigReference.FromComposer(authoring.CameraRig),CameraTargetSourceDescriptor.ExplicitTransform(authoring.FollowTarget,$"Prepared Player camera target {slot.Value.Value}"),new CameraRequestPolicy(authoring.Precedence,$"player.camera:{slot.Value.Value}"),CameraRequestReleaseCondition.ExplicitRelease,source,$"{reason}; actor='{preparation.Materialization.ActorId.StableText}'");
            if(!created.IsSucceeded) return Reject(PlayerGameplayCameraEligibilityStatus.RejectedRigConfiguration,op,slot,previous,created.BlockingIssue);
            var publisherResult=LocalPlayerCameraRequestPublisher.Create(output,created.Request);
            if(!publisherResult.Succeeded || publisherResult.Publisher is not LocalPlayerCameraRequestPublisher publisher) return Reject(PlayerGameplayCameraEligibilityStatus.RejectedRigConfiguration,op,slot,previous,publisherResult.DiagnosticSummary);
            var published=publisher.Publish(); if(!published.Succeeded || !publisher.IsPublished) return Reject(PlayerGameplayCameraEligibilityStatus.RejectedRigConfiguration,op,slot,previous,published.DiagnosticSummary);
            var summary=Create(preparation,authoring,token,requestId,true,false,source,reason,"Player camera request is published on the expected output.");
            if(!summary.IsValid) return Reject(PlayerGameplayCameraEligibilityStatus.RejectedRigConfiguration,op,slot,previous,"Camera publication produced incoherent capability evidence.");
            _slots[slot]=summary; _records[slot]=new Record { publisher=publisher, request=created.Request, authoring=authoring }; _revision++; return Result(PlayerGameplayCameraEligibilityStatus.SucceededEligible,op,slot,previous,summary,summary.Message);
        }
        internal PlayerGameplayCameraEligibilityResult TrySkipOptional(PlayerActorPreparationSummary preparation, PlayerGameplayOccupancySummary ignoredOccupancy, PlayerGameplayInputBindingSummary ignoredInput, CameraOutputSessionBinding outputSession, PlayerGameplayCameraRequiredness requiredness, string source, string reason)
        {
            const string op="SkipOptionalCamera"; var slot=preparation.PlayerSlotId; var previous=Get(slot);
            if(requiredness!=PlayerGameplayCameraRequiredness.Optional) return Reject(PlayerGameplayCameraEligibilityStatus.RejectedOptionalSkipRequired,op,slot,previous,"Only an Optional Player camera may be skipped.");
            if(!ValidatePreparation(preparation,slot,out var issue) || !TryGetOutput(outputSession,out var output,out issue)) return Reject(PlayerGameplayCameraEligibilityStatus.RejectedInvalidRequest,op,slot,previous,issue);
            if(previous.HasCurrentDecision) return previous.IsSkippedOptional && previous.PreparationToken==preparation.Token && previous.Token.CameraOutputId==output.OutputId.ToString() ? Result(PlayerGameplayCameraEligibilityStatus.SucceededAlreadySkipped,op,slot,previous,previous,"Optional camera is already skipped.") : Reject(PlayerGameplayCameraEligibilityStatus.RejectedSlotAlreadyEvaluated,op,slot,previous,"Player Slot already has another current camera capability.");
            var token=new PlayerGameplayCameraEligibilityToken(_sessionContextId,slot,preparation.Token,output.OutputId.ToString(),++_sequence);
            var summary=new PlayerGameplayCameraEligibilitySummary(_sessionContextId,slot,PlayerGameplayCameraEligibilityState.SkippedOptional,requiredness,preparation.PreparedActorProfileId,preparation.Materialization.ActorId,preparation.Materialization.Owner,preparation.Materialization.RuntimeContentIdentity,preparation.Token,token,string.Empty,string.Empty,string.Empty,0,string.Empty,string.Empty,string.Empty,false,string.Empty,true,token.CameraRevision,source,reason,"Optional Player camera was skipped without publication.");
            _slots[slot]=summary; _revision++; return Result(PlayerGameplayCameraEligibilityStatus.SucceededSkippedOptional,op,slot,previous,summary,summary.Message);
        }
        internal PlayerGameplayCameraEligibilityResult TryRelease(PlayerSlotId slot, PlayerGameplayCameraEligibilityToken expected, string source, string reason)
        {
            const string op="ReleaseCameraEligibility"; var previous=Get(slot); if(previous.IsNotEvaluated) return Result(PlayerGameplayCameraEligibilityStatus.SucceededAlreadyReleased,op,slot,previous,previous,"Camera capability is already released.");
            if(!expected.IsValid || previous.Token!=expected) return Reject(PlayerGameplayCameraEligibilityStatus.RejectedForeignOrStaleEligibility,op,slot,previous,"Camera release requires the exact current capability token.");
            if(previous.IsEligible && _records.TryGetValue(slot,out var record) && record.publisher!=null) { var release=record.publisher.Release(); if(!release.Succeeded || record.publisher.IsPublished) return Reject(PlayerGameplayCameraEligibilityStatus.RejectedForeignOrStaleEligibility,op,slot,previous,$"Camera request release failed and remains retryable. {release.DiagnosticSummary}"); }
            _records.Remove(slot); var current=PlayerGameplayCameraEligibilitySummary.NotEvaluated(_sessionContextId,slot,previous.CameraRevision,source,reason,"Player camera capability released."); _slots[slot]=current; _revision++; return Result(PlayerGameplayCameraEligibilityStatus.SucceededReleased,op,slot,previous,current,current.Message);
        }
        internal PlayerGameplayCameraEligibilitySnapshot CreateSnapshot() { var ordered=new PlayerGameplayCameraEligibilitySummary[_orderedSlots.Length]; for(int i=0;i<ordered.Length;i++) ordered[i]=_slots[_orderedSlots[i]]; return new PlayerGameplayCameraEligibilitySnapshot(_sessionContextId,_revision,ordered,_lastOperationStatus,_lastOperationMessage); }
        private bool ValidatePreparation(PlayerActorPreparationSummary p, PlayerSlotId slot,out string issue) { issue=string.Empty; if(!p.IsValid || !p.IsPrepared || !p.Materialization.IsActive || !slot.IsValid || !_slots.ContainsKey(slot) || !string.Equals(p.SessionContextId,_sessionContextId,StringComparison.Ordinal)) { issue="Camera capability requires a current prepared Player in this Session."; return false; } return true; }
        private static bool TryGetOutput(CameraOutputSessionBinding binding,out CameraOutputSession output,out string issue) { output=null; issue=string.Empty; return binding!=null && binding.TryGetSession(out output,out issue); }
        private PlayerGameplayCameraEligibilitySummary Create(PlayerActorPreparationSummary p,PlayerGameplayCameraAuthoring a,PlayerGameplayCameraEligibilityToken t,string request,bool published,bool released,string source,string reason,string message) => new PlayerGameplayCameraEligibilitySummary(_sessionContextId,p.PlayerSlotId,PlayerGameplayCameraEligibilityState.Eligible,a.Requiredness,p.PreparedActorProfileId,p.Materialization.ActorId,p.Materialization.Owner,p.Materialization.RuntimeContentIdentity,p.Token,t,a.CameraRig.name,a.FollowTarget.name,a.LookAtTarget!=null?a.LookAtTarget.name:string.Empty,a.Precedence,request,$"player.camera:{_sessionContextId}:{p.PlayerSlotId.Value.Value}:{t.CameraRevision}",$"player.camera:{p.PlayerSlotId.Value.Value}",published,nameof(PlayerGameplayCameraEligibilityRuntimeContext),released,t.CameraRevision,source,reason,message);
        private PlayerGameplayCameraEligibilitySummary Get(PlayerSlotId slot) => slot.IsValid && _slots.TryGetValue(slot,out var summary)?summary:default;
        private PlayerGameplayCameraEligibilityResult Reject(PlayerGameplayCameraEligibilityStatus status,string op,PlayerSlotId slot,PlayerGameplayCameraEligibilitySummary previous,string message) => Result(status,op,slot,previous,previous,message);
        private PlayerGameplayCameraEligibilityResult Result(PlayerGameplayCameraEligibilityStatus status,string op,PlayerSlotId slot,PlayerGameplayCameraEligibilitySummary previous,PlayerGameplayCameraEligibilitySummary current,string message) { _lastOperationStatus=status; _lastOperationMessage=message.NormalizeText(); return new PlayerGameplayCameraEligibilityResult(status,op,slot,previous,current,CreateSnapshot(),_lastOperationMessage); }
    }
}
