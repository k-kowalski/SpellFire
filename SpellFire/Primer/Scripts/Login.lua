Args = {{
	Login = '{0}',
	Password = '{1}',
	Character = '{2}'
}}

function Main()
	if(AccountLoginUI and AccountLoginUI:IsVisible()) then
		DefaultServerLogin(Args['Login'], Args['Password'])
	elseif(CharacterSelectUI and CharacterSelectUI:IsVisible()) then
		for charIndex = 0, GetNumCharacters() do
			if (GetCharacterInfo(charIndex) == Args['Character']) then
				CharacterSelect_SelectCharacter(charIndex)
				EnterWorld()
			end
		end
	end
end

Main()