using SwiftlyS2.Shared.Players;

namespace zAutoRestart7GC.Services;

public interface IVoteExtendManager
{
  bool CanVoteNow(out string reasonKey, out object[] reasonArgs);
  bool TryVote(IPlayer player, out string feedbackKey, out object[] feedbackArgs);
  void OnClientDisconnected(int playerId);
  void ResetVotes();
}
