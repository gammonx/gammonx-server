# disconnect handling/cases
## MatchNotStarted
- Player disconnects and reconnects 		> OK :: PlayerConnectionId Updated > JoinMatch command
- Player disconnects and timer runs out 	> OK :: Force Disconnect Event for other Player
- Both Player disconnects					> OK :: Both Timers run out > nothing happens
## MatchStarted
- Player disconnects and reconnects		    > OK :: PlayerConnectionId Updated > Match-/GameState command
- Player disconnects and timer runs out	    > OK :: Disconnected player loses the match
- Both Player disconnects					> OK :: First player who reaches the grace period loses the match
## MatchFinished
- Player disconnects and reconnects		    > OK :: Match was cleaned up > Force Disconnect Event > late disconnect callback is ignored
- Player disconnects and timer runs out	    > OK :: nothing > turn and disconnect timers were canceled during cleanup
- Both Player disconnects					> OK :: nothing

## MatchEnded cleanup
- `MatchEnded` processing enqueues the completed game, match, and statistics results before the terminal state is sent.
- Cleanup sends `ForceDisconnectEvent`, removes the match and player connection records, removes both connections from the group, and cancels the remaining timers.
- SignalR may invoke `OnDisconnectedAsync` after cleanup has removed the player connection record. This is an expected idempotent callback and must not be logged as an error or start a disconnect grace timer.