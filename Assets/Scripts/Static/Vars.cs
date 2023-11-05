public class Vars
{
    public const int NOISE_THREAD_SIZE = 8; // Ne pas oublier d'aussi changer la valeur dans 'Consts.cginc'
    public static bool DEBUG_MODE = false;
    public static int BASE_FLORA_DENSITY = 1000;
    public static int[] FLORA_DENSITY = new int[] { 1000, 400, 200, 400, 100 };
    public static int GROUNDFAUNA_DENSITY = 20;
    public static int REF_CHUNK_TRI_COUNT = 7000; // Utilisée pour l'adaptation de la densité de la flore
}