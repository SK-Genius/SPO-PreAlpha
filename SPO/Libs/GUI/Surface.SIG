[§INTERFACE tSelf [<
	Size: [tSelf : [] => [§LOAD "../Size.SIG"]]
	ClearWith...: [tSelf : [§INT, §INT, §INT] => []]
	Fill...With...: [tSelf : [[§LOAD "Rect.SIG"], [§INT, §INT, §INT]] => []]
	Stroke...With...: [tSelf : [[§LOAD "Rect.SIG"], [§INT, §INT, §INT]] => []]
	Draw...At...InColor...: [tSelf : [§TEXT, [§INT, §INT], [§INT, §INT, §INT]] => []]
	ClipTo...: [tSelf : [§LOAD "Rect.SIG"] => []]

	ResetClip: [tSelf : [] => []]
	SetTitle...: [tSelf : §TEXT => []]
>]]