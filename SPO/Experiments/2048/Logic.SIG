[<
	BoardSize: §INT
	NewGameWithSeed...: [§INT => [§LOAD "State.SIG"]]
	...AfterMove...: [[[§LOAD "State.SIG"], [§LOAD "../../Apps/2048/Direction.SIG"]] => [§LOAD "State.SIG"]]
	...TileAt...: [[[§LOAD "Board.SIG"], [§INT, §INT]] => §INT]
	...CanMove: [[§LOAD "Board.SIG"] => §BOOL]
	...LargestTile: [[§LOAD "Board.SIG"] => §INT]
>]