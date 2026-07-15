# 12 — Player Slot / Join Hygiene

Status: Implemented source cut; manual regression pending
Last updated: 2026-07-15

H0-H4 removed the passive Slot graph, the F49/F51/F52 subgraph and the old
PlayerInputManager surface. H5 remains a manual import/compile/smoke gate. H6
FIRSTGAME migration must not begin before P3K.7I confirms 16/16.
