[<
	BoardSize: §INT
	NewGameWithSeed...: [§INT => [§LOAD "State.SIG"]]
	...AfterMove...: [[[§LOAD "State.SIG"], [§LOAD "Direction.SIG"]] => [§LOAD "State.SIG"]]
	...TileAt...: [[[§LOAD "Board.SIG"], [§INT, §INT]] => §INT]
	...CanMove: [[§LOAD "Board.SIG"] => §BOOL]
	...LargestTile: [[§LOAD "Board.SIG"] => §INT]
>]