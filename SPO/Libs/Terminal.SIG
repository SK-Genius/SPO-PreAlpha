[§INTERFACE tSelf [<
	Size: [tSelf : [] => [§LOAD "Size.SIG"]]
	MoveCursorTo...: [tSelf : [§INT, §INT] => []]
	IsCursorVisible: [tSelf : [] => §BOOL]
	ShowCursor: [tSelf : [] => []]
	HideCursor: [tSelf : [] => []]
	Draw...At...WithStyle...: [
		tSelf : [
			§TEXT
			[§INT, §INT]
			[§LOAD "TextStyle.SIG"]
		] => []
	]
	Clear: [tSelf : [] => []]
>]]