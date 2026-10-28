[<
	Initial: [§LOAD "State.SIG"]
	EmptyPanel: [§LOAD "Panel.SIG"]
	BeginFor...With...: [
		[§VAR [§LOAD "Panel.SIG"]] : [[§LOAD "Event.SIG"], [§LOAD "State.SIG"]] => []
	]
	AddText...InColor...: [
		[§VAR [§LOAD "Panel.SIG"]] : [§TEXT, [§INT, §INT, §INT]] => []
	]
	AddParagraph...WithAlignment...InColor...: [
		[§VAR [§LOAD "Panel.SIG"]] : [§TEXT, [§LOAD "Alignment.SIG"], [§INT, §INT, §INT]] => []
	]
	AddButton...WithText...Selected...: [[§VAR [§LOAD "Panel.SIG"]] : [§INT, §TEXT, §BOOL] => §BOOL]
	AddTextInput...WithLabel...Value...: [[§VAR [§LOAD "Panel.SIG"]] : [§INT, §TEXT, §TEXT] => §TEXT]
	AddListWith...Do...: [
		§GENERIC tResult [[§VAR [§LOAD "Panel.SIG"]] : [
			[§LOAD "ListStyle.SIG"]
			[[§VAR [§LOAD "Panel.SIG"]] : [] => tResult]
		] => tResult]
	]
	AddListWith...DoWithContext...: [
		§GENERIC tContext [§GENERIC tResult [[[§VAR [§LOAD "Panel.SIG"]], tContext] : [
			[§LOAD "ListStyle.SIG"]
			[[[§VAR [§LOAD "Panel.SIG"]], tContext] : [] => tResult]
		] => tResult]]
	]
	FinishWith...In...: [
		[§VAR [§LOAD "Panel.SIG"]] : [
			[§LOAD "ListStyle.SIG"]
			[§LOAD "../Size.SIG"]
		] => [[§LOAD "State.SIG"], [§LOAD "Frame.SIG"]]
	]
	...LayoutWithin...: [[[§LOAD "Element.SIG"], [§LOAD "Rect.SIG"]] => [§LOAD "Frame.SIG"]]
	Draw...With...: [
		[§LOAD "Surface.SIG"] : [
			[§LOAD "Frame.SIG"]
			[§LOAD "Interaction.SIG"]
		] => []
	]
>]