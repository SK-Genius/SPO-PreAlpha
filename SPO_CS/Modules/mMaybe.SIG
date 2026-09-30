[
	§SIG_WITH F € [§LOAD "../Types/TypeConstructor.SIG"] IN [<
		Some...: [§GENERIC t [t => [.F t]]]
		None...: [§GENERIC t [t => [.F t]]]
		...>>...: [
			§GENERIC tIn [
				§GENERIC tOut [
					[
						.F tIn
						tIn => tOut
					] => [.F tOut]
				]
			]
		]
		...>>?...: [
			§GENERIC tIn [
				§GENERIC tOut [
					[
						.F tIn
						tIn => [.F tOut]
					] => [.F tOut]
				]
			]
		]
		...Or...: [
			§GENERIC t [
				[
					.F t
					t
				] => t
			]
		]
		...OrDo...: [
			§GENERIC t [
				[
					.F t
					[] => t
				] => t
			]
		]
		...OrTry...: [
			§GENERIC t [
				[
					.F t
					[] => [.F t]
				] => [.F t]
			]
		]
	>]
]
