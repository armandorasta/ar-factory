# Terminology:
- `unit`: a block that operates on items that pass through it.
- `item`: a moving block that gets operated on by `units`.
- `spot`: a square on the world panel.
- `tick`: the smallest amount of time that can pass as the game operates in discrete time points.
- `command`: what the `units` tell the world panel to do to run the simulation.


# Units
## One Slot Tools
1. Supplier
2. Slider
3. Adder
4. Updater
5. Key & Lock.
6. Barrier.
7. Branch.
8. Bin.

## More Than One Slot:
9. Combiner
10. Cloner


# Commands
1. Slide
2. Update
3. Spawn
4. Kill
5. Teleport
6. SetLocked
7. CondSlide

I could add more, like Clone for example, but I could just use Spawn to do the same thing...
