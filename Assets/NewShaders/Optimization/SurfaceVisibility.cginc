#ifndef VRC_MINE_SURFACE_VISIBILITY
#define VRC_MINE_SURFACE_VISIBILITY
bool ShouldDrawFaceOneWay(uint firstID, uint secondID)
{
    if (firstID == 0u) return secondID != 0u; // air
    if (firstID > 8u) return false; // block
    if (firstID >= 2u && firstID <= 8u) return true; // leaf
    if (firstID == 1u) return secondID != 1u; // glass
    return false;
}
#endif
