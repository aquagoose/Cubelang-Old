# LISTS----------------------------------
list is List("hello!", 7, "my list!", "stuff", 6.5)
log("The list: {list}")
log("Third element in the list: {list[2]}")
log("Last element in the list: {list[-1]}")
log("Random element: {list[randI32(0, len(list))]}")
addElement(list, "test")
log("The list with our new element: {list}")
log("List containing the number 7 is {contains(list, 7)}")

# DICTIONARIES--------------------------
dict is Dict()
dict["item1"] is "This is an item."
dict["test"] is 3
dict["anotherdict"] is Dict()
dict["anotherdict"]["item8"] is "Random item"
log("The dictionary: {dict}")
# For now you need to use backslashes for this cause the bodgy interpreter isn't smart
# enough to pick that up yet. Still works.
log("Element \"test\": {dict[\"test\"]}")
log("anotherdict: {dict[\"anotherdict\"]}")
