
public static class QlearnVariables
{
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ SPELLS ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    public static int SPELL_PA = 3;
    public static int LONG_RANGE_SPELL_DAMAGE = 10;
    public static int SHORT_RANGE_SPELL_DAMAGE = 30;
    public static int HEAL_SPEAL_LIFE = 10;


    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ REWARDS ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    public static int NERIENFAIRE = 0;
    public static int RED1 = 0;
    public static int RED2 = 0;
    public static int RED3 = 0;
    public static int AUG1 = 0;
    public static int AUG2 = 0;
    public static int AUG3 = 0;
    public static int REW_LONGRANGE = 0; //0 * LONG_RANGE_SPELL_DAMAGE;
    public static int REW_DAMAGECAC = 0; //0 * SHORT_RANGE_SPELL_DAMAGE;
    public static int REW_HEAL = 0; //0 * HEAL_SPEAL_LIFE / 3;
    public static int WIN = 1;


    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ TRAIN ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    public static bool TRAINABLE = true;
    public static double EPSILON = 0.99;
    public static double COEFF_EPSILON = 0.996;
    public static double COEFF_TRAIN_BACKTRACE = 0.001; //Facteur apprentissage
    public static double COEFF_TRAIN_UPDATE = 1; //Facteur actualisation
    public static double EPSILON_MIN = 0.1;
    public static int TRAINING_RATIO = 10;
    public static int NUMBER_OF_TRAINING_GAMES = 1000000;
    public static int NUMBER_OF_TEST_GAMES = 500;
}
