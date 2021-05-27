
//Rewards according to the played action, for the training
// Hardcoded for the moment
public static class Rewards
{
    private static int rienfaire = QlearnVariables.NERIENFAIRE;
    private static int red1 = QlearnVariables.RED1;
    private static int red2 = QlearnVariables.RED2;
    private static int red3 = QlearnVariables.RED3;
    private static int aug1 = QlearnVariables.AUG1;
    private static int aug2 = QlearnVariables.AUG2;
    private static int aug3 = QlearnVariables.AUG3;
    private static int damageLongRange = QlearnVariables.REW_LONGRANGE;
    private static int damageCac = QlearnVariables.REW_DAMAGECAC;
    private static int heal = QlearnVariables.REW_HEAL;

    public static int[] tab = new int[] { rienfaire, red1, red2, red3, aug1, aug2, aug3, damageLongRange, damageCac, heal };
    public static int win = QlearnVariables.WIN;
}
