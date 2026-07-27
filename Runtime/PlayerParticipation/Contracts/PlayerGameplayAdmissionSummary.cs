using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
namespace Immersive.Framework.PlayerParticipation
{
 [FrameworkApiStatus(FrameworkApiStatus.Experimental,"P3K.5 immutable Player gameplay admission summary.")]
 public readonly struct PlayerGameplayAdmissionSummary
 {
  internal PlayerGameplayAdmissionSummary(string session,PlayerSlotId slot,PlayerGameplayAdmissionState state,ActorProfileId profile,ActorId actor,RuntimeContentOwner owner,RuntimeContentIdentity identity,PlayerActorPreparationToken preparation,PlayerGameplayOccupancyToken occupancy,PlayerGameplayInputBindingToken input,PlayerGameplayCameraEligibilityToken camera,PlayerGameplayAdmissionToken token,int revision,string source,string reason,string message){SessionContextId=session.NormalizeText();PlayerSlotId=slot;State=state;ActorProfileId=profile;ActorId=actor;Owner=owner;RuntimeContentIdentity=identity;PreparationToken=preparation;OccupancyToken=occupancy;InputBindingToken=input;CameraEligibilityToken=camera;Token=token;AdmissionRevision=revision;Source=source.NormalizeText();Reason=reason.NormalizeText();Message=message.NormalizeText();}
  public string SessionContextId{get;} public PlayerSlotId PlayerSlotId{get;} public PlayerGameplayAdmissionState State{get;} public ActorProfileId ActorProfileId{get;} public ActorId ActorId{get;} public RuntimeContentOwner Owner{get;} public RuntimeContentIdentity RuntimeContentIdentity{get;} public PlayerActorPreparationToken PreparationToken{get;} public PlayerGameplayOccupancyToken OccupancyToken{get;} public PlayerGameplayInputBindingToken InputBindingToken{get;} public PlayerGameplayCameraEligibilityToken CameraEligibilityToken{get;} public PlayerGameplayAdmissionToken Token{get;} public int AdmissionRevision{get;} public string Source{get;} public string Reason{get;} public string Message{get;}
  public bool IsNotAdmitted=>State==PlayerGameplayAdmissionState.NotAdmitted; public bool IsReady=>State==PlayerGameplayAdmissionState.Ready; public bool IsBlockedByInputGate=>State==PlayerGameplayAdmissionState.BlockedByInputGate; public bool IsReleaseFailed=>State==PlayerGameplayAdmissionState.ReleaseFailed; public bool IsAdmitted=>IsReady||IsBlockedByInputGate||IsReleaseFailed; public bool GameplayReady=>IsReady;
  public bool IsValid=>!string.IsNullOrEmpty(SessionContextId)&&PlayerSlotId.IsValid&&State!=PlayerGameplayAdmissionState.None&&(IsNotAdmitted?!Token.IsValid:Token.IsValid&&OccupancyToken.IsValid&&InputBindingToken.IsValid&&CameraEligibilityToken.IsValid&&Token.SessionContextId==SessionContextId&&Token.PlayerSlotId==PlayerSlotId);
  public string ToDiagnosticString()=>$"session='{SessionContextId}' slot='{PlayerSlotId.StableText}' state='{State}' admission='{Token.StableText}' occupancy='{OccupancyToken.StableText}' input='{InputBindingToken.StableText}' camera='{CameraEligibilityToken.StableText}'";
  internal static PlayerGameplayAdmissionSummary NotAdmitted(string session,PlayerSlotId slot,int revision,string source,string reason,string message)=>new PlayerGameplayAdmissionSummary(session,slot,PlayerGameplayAdmissionState.NotAdmitted,default,default,default,default,default,default,default,default,default,revision,source,reason,message);
 }
}
