public static class FloorActivationCheck
{
    public static bool IsActive(FloorDefinition ownerFloor, FloorDefinition currentFloor)
    {
        if (ownerFloor == null) return true; // sem Floor dono definido = sempre ativo (compatibilidade com objetos que não pertencem a nenhum Floor específico)
        return ownerFloor == currentFloor;
    }
}
