-- Every order the game names: OrderId2String over 851968 to 852800 (game-facts: 373 ids have a name), written 8 to a line.
local n, row = 0, {}
for i = 851968, 852800 do
  local s = OrderId2String(i)
  if s ~= nil and s ~= "" then
    n = n + 1
    row[#row + 1] = i .. "=" .. s
    if #row == 8 then out(table.concat(row, " ")); row = {} end
  end
end
if #row > 0 then out(table.concat(row, " ")) end
out("named " .. n)
done()
