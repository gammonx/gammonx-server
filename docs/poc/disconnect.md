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
- Player disconnects and reconnects		    > OK :: error > no match to retrieve
- Player disconnects and timer runs out	    > OK :: nothing
- Both Player disconnects					> OK :: nothing