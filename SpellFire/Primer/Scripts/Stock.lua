function UseBagItem(filter)
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

function UseItemByName(providedItemName)
	myBagFilter = function(itemName) itemName == providedItemName end
	UseBagItem(myBagFilter)
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
