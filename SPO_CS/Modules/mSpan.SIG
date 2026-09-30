[
	§SIG_WITH F € [§LOAD "../Types/TypeConstructor.SIG"] IN [<
		...--...: [§GENERIC t [[t, t] => [.F t]]]
		SpanAt...: [§GENERIC t [t => [.F t]]]
		...Start: [§GENERIC t [[.F t] => t]]
		...End: [§GENERIC t [[.F t] => t]]
		...&...With...: [
			§GENERIC t [[.F t, .F t, [t, t] => §BOOL] => [.F t]]
		]
		...==...With...: [
			§GENERIC t [
				[
					.F t
					.F t
					[t, t] => §BOOL
				] => §BOOL
			]
		]
		==With...: [
			§GENERIC t [
				[[t, t] => §BOOL] => [[.F t, .F t] => §BOOL]
			]
		]
	>]
]
