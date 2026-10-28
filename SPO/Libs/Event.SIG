[
	[
		#Tick §INT
	] | [
		#Key [<
			Key: [§LOAD "Key.SIG"]
			Character: §CHAR
			IsDown: §BOOL
			IsRepeat: §BOOL
			RepeatCount: §INT
			Modifiers: [§LOAD "Modifiers.SIG"]
		>]
	] | [
		#Mouse [<
			Position: [§INT, §INT]
			Buttons: [§LOAD "MouseButtons.SIG"]
			PressedButtons: [§LOAD "MouseButtons.SIG"]
			ReleasedButtons: [§LOAD "MouseButtons.SIG"]
			WheelDelta: §INT
			IsHorizontalWheel: §BOOL
			HasMoved: §BOOL
			IsDoubleClick: §BOOL
			Modifiers: [§LOAD "Modifiers.SIG"]
		>]
	] | [
		#Resize [§LOAD "Size.SIG"]
	] | [
		#Focus §BOOL
	]
]
