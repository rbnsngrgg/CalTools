def search(text, items, mode):
    textsplit = text.split()
    terms = []
    nots = []
    ands = []
    ors = []
    foundItems = []
    itemList = []
    #Prevents first item being dropped by sqlite cursor
    for item in items:
        itemList.append(item)
    #Find keywords
    for index,term in enumerate(textsplit):
        if '""' in term[0:2] and '""' in term[-2:] == '""':
            terms.append(term.strip('"'))
    for index,term in enumerate(textsplit):
        if term.strip('"') == 'not' and len(textsplit) >= 2 and textsplit[-1] != term:
            if textsplit[index + 1].strip('"') in textsplit:
                nots.append(textsplit[index + 1].strip('"'))
            if textsplit[0] != term and textsplit[1] == term:
                ands.append(textsplit[0])
        elif len(textsplit) > 2 and (term == '""and""' or term == '""or""'):
            newand = []
            newor = []
            if index - 1 >= 0:
                if term.strip('"') == 'and':
                    ands.append(textsplit[index - 1].strip('"'))
                else:
                    newor.append(textsplit[index - 1].strip('"'))
            if index + 1 <= len(textsplit):
                if term.strip('"') == 'and':
                    ands.append(textsplit[index + 1].strip('"'))
                else:
                    newor.append(textsplit[index + 1].strip('"'))
                    ors.append(newor)
    #Find operands for keywords
    for item in itemList:
        if len(nots) == 0 and len(ands) == 0 and len(ors) == 0:
            break
        hasNot = False
        andCount = 0
        hasOr = False
        andsFound = True
        for keyword in nots:
            if keyword.lower() in item[mode].lower():
                hasNot = True
                break
        if hasNot == True:
            continue
        for keyword in ands:
            if keyword.lower() in item[mode].lower():
                andCount += 1
        if andCount != len(ands):
            continue
        else:
            andsFound = True
        for pair in ors:
            for keyword in pair:
                if keyword.lower() in item[mode].lower():
                    hasOr =True
        if len(ors) == 0:
            hasOr = True
        if hasNot == False:
            if andsFound == True and hasOr == True:
                foundItems.append(item)
    if len(terms) == 0:
        for item in itemList:
            if text.lower() in item[mode].lower():
                foundItems.append(item)
    return foundItems