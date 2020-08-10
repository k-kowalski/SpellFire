targetItemID = 8077 --mineral

targetCount = 12
currentCount = 0

function InitStock()
	bagCount = 4
	for bagIndex = 0, bagCount do
		local bagName = GetBagName(bagIndex)
		if bagName then
			slotCount = GetContainerNumSlots(bagIndex)
			for slotIndex = 0, slotCount do
				local itemID = GetContainerItemID(bagIndex, slotIndex)
				if itemID == targetItemID then
					texture, count, locked, quality, readable, lootable, link = GetContainerItemInfo(bagIndex, slotIndex)
					currentCount = currentCount + count
				end
			end
		end
	end

	-- after processing conclude
	if currentCount >= targetCount then
		return 1
	else
		CastSpellByName('Conjure Water(Rank 3)')
		return 0
	end
end

function TradeStock()
	bagCount = 4
	for bagIndex = 0, bagCount do
		local bagName = GetBagName(bagIndex)
		if bagName then
			slotCount = GetContainerNumSlots(bagIndex)
			for slotIndex = 0, slotCount do
				local itemID = GetContainerItemID(bagIndex, slotIndex)
				if itemID then
					local itemName = GetItemInfo(itemID)
					if itemName:find('Conjured') and itemName:find('Water') then
						UseContainerItem(bagIndex, slotIndex)
						return
					end
				end
			end
		end
	end
end

if TradeFrame:IsShown() then
	TradeStock()
end
