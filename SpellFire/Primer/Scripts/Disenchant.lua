Quality = {
	Uncommon = 2,
}

function Main()
	bagCount = 4
	for bagInd = 0, bagCount do
		local bagName = GetBagName(bagInd)
		if bagName then
			DisenchantBagContent(bagInd)
		end
	end
end

function DisenchantBagContent(bagIndex)
	slotCount = GetContainerNumSlots(bagIndex)
	for slotInd = 0, slotCount do
		local itemID = GetContainerItemID(bagIndex, slotInd)
		if itemID then
			DisenchantItem(itemID)
		end
	end
	
	return false
end

function DisenchantItem(itemID)
	local name, link, quality, iLevel, reqLevel, class, subclass, maxStack, equipSlot, texture, vendorPrice = GetItemInfo(itemID)
	if class == "Weapon" or class == "Armor" then
		if quality == Quality["Uncommon"] then
			CastSpellByName('Disenchant')
			SpellTargetItem(itemID)
		end
	end
	
end

Main()