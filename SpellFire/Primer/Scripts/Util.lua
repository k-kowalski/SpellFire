function sfUseBagItem(filter)
	bagCount = 4
	for bagIndex = 0, bagCount do
		local bagName = GetBagName(bagIndex)
		if bagName then
			slotCount = GetContainerNumSlots(bagIndex)
			for slotIndex = 0, slotCount do
				local itemID = GetContainerItemID(bagIndex, slotIndex)
				if itemID then
					local itemName = GetItemInfo(itemID)
					if filter(itemName) then
						UseContainerItem(bagIndex, slotIndex)
						return
					end
				end
			end
		end
	end
end

function sfUseInventoryItem(filter)
	invSlotCount = 19
	for invSlot = 0, invSlotCount do
		local itemID = GetInventoryItemID('player', invSlot)
		if itemID then
			local itemName = GetItemInfo(itemID)
			if filter(itemName) then
				UseInventoryItem(invSlot)
				return
			end
		end
	end
end
