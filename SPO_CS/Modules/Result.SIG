[
	§SIG_WITH F € [§TYPE => [§LOAD "../Types/TypeConstructor.SIG"]] IN [<
		OK...: [
			§GENERIC tError [
				§GENERIC tValue [
					[tError, tValue] => [.(.F tError) tValue]
				]
			]
		]
		Fail...: [
			§GENERIC tError [
				§GENERIC tValue [
					[tError, tValue] => [.(.F tError) tValue]
				]
			]
		]
		...>>...: [
			§GENERIC tError [
				§GENERIC tIn [
					§GENERIC tOut [
						[
							[.(.F tError) tIn]
							tIn => tOut
						] => [.(.F tError) tOut]
					]
				]
			]
		]
		...Or...: [
			§GENERIC tError [
				§GENERIC tValue [
					[
						[.(.F tError) tValue]
						tValue
					] => tValue
				]
			]
		]
	>]
]
