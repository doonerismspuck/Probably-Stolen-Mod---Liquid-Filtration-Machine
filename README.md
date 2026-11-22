# Probably Stolen Mod - Liquid Filtration Machine
A mod for the game Probably Stolen. 

Adds a new machine that takes two water containers and a filter (water or kotton), then it runs liquid from one container through the filter and into the other container. It has been added to the inventory of Dr. Jackson, the retired aquafarmer and the retired chemist, and you can only have one of this machine.

Extremely compatible, I'd be genuinely surprised if this mod broke something. It works by taking the work that the game's HandleFilter method does and instead simulates it in different batches until it finds a valid one, then commits that as a change to the game. I had to do it this way so that the mod remained lightweight, otherwise it would call a bunch of methods hundred of times.

Currently uses the RAS as placeholder art.