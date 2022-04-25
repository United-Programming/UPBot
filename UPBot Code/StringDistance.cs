using System;

public static class StringDistance {
  /* The Winkler modification will not be applied unless the 
   * percent match was at or above the mWeightThreshold percent 
   * without the modification. 
   * Winkler's paper used a default value of 0.7
   */
  private static readonly double mWeightThreshold = 0.55;

  /* Size of the prefix to be concidered by the Winkler modification. 
   * Winkler's paper used a default value of 4
   */
  private static readonly int mNumChars = 5;


  /// <summary>
  /// Returns the Jaro-Winkler distance between the specified strings. 
  /// The distance is symmetric and will fall in the range 0 (perfect match) to 1 (no match). 
  /// </summary>
  /// <param name="aString1">First String</param>
  /// <param name="aString2">Second String</param>
  /// <returns></returns>
  public static double JWDistance(string aString1, string aString2) {
    return 1.0 - JWProximity(aString1, aString2);
  }


  /// <summary>
  /// Returns the Jaro-Winkler distance between the specified strings. The distance is symmetric and will fall in the range 0 (no match) to 1 (perfect match). 
  /// </summary>
  /// <param name="aString1">First String</param>
  /// <param name="aString2">Second String</param>
  /// <returns></returns>
  public static double JWProximity(string aString1, string aString2) {
    int lLen1 = aString1.Length;
    int lLen2 = aString2.Length;
    if (lLen1 == 0) return lLen2 == 0 ? 1.0 : 0.0;

    int lSearchRange = Math.Max(0, Math.Max(lLen1, lLen2) / 2 - 1);

    // default initialized to false
    bool[] lMatched1 = new bool[lLen1];
    bool[] lMatched2 = new bool[lLen2];

    int lNumCommon = 0;
    for (int i = 0; i < lLen1; ++i) {
      int lStart = Math.Max(0, i - lSearchRange);
      int lEnd = Math.Min(i + lSearchRange + 1, lLen2);
      for (int j = lStart; j < lEnd; ++j) {
        if (lMatched2[j]) continue;
        if (aString1[i] != aString2[j])
          continue;
        lMatched1[i] = true;
        lMatched2[j] = true;
        ++lNumCommon;
        break;
      }
    }
    if (lNumCommon == 0) return 0.0;

    int lNumHalfTransposed = 0;
    int k = 0;
    for (int i = 0; i < lLen1; ++i) {
      if (!lMatched1[i]) continue;
      while (!lMatched2[k]) ++k;
      if (aString1[i] != aString2[k]) ++lNumHalfTransposed;
      ++k;
    }
    // System.Diagnostics.Debug.WriteLine("numHalfTransposed=" + numHalfTransposed);
    int lNumTransposed = lNumHalfTransposed / 2;

    // System.Diagnostics.Debug.WriteLine("numCommon=" + numCommon + " numTransposed=" + numTransposed);
    double lNumCommonD = lNumCommon;
    double lWeight = (lNumCommonD / lLen1
                     + lNumCommonD / lLen2
                     + (lNumCommon - lNumTransposed) / lNumCommonD) / 3.0;

    if (lWeight <= mWeightThreshold) return lWeight;
    int lMax = Math.Min(mNumChars, Math.Min(aString1.Length, aString2.Length));
    int lPos = 0;
    while (lPos < lMax && aString1[lPos] == aString2[lPos])
      ++lPos;
    if (lPos == 0) return lWeight;
    return lWeight + 0.1 * lPos * (1.0 - lWeight);
  }

  internal static int CountSubparts(string a, string b) {
    try {
      int al = a.Length;
      int bl = b.Length;
      int num = al + bl;
      for (int len = al; len >= 3; len--) {
        for (int i = 0; i <= al - len; i++) {
          if (b.IndexOf(a.Substring(i, len)) != 0) {
            num -= len;
            len = 0;
            break;
          }
        }
      }
      for (int len = bl; len >= 3; len--) {
        for (int i = 0; i < bl - len; i++) {
          if (a.IndexOf(b.Substring(i, len)) != 0) {
            num -= len;
            len = 0;
            break;
          }
        }
      }
      return num;
    } catch (Exception) {
      return 10000000;
    }
  }

  /// <summary>
  /// Damerau-Levenshtein string distance
  /// </summary>
  /// <param name="s"></param>
  /// <param name="t"></param>
  /// <returns></returns>
  public static int DLDistance(string s, string t) {
    var bounds = new { Height = s.Length + 1, Width = t.Length + 1 };

    int[,] matrix = new int[bounds.Height, bounds.Width];

    for (int height = 0; height < bounds.Height; height++) { matrix[height, 0] = height; };
    for (int width = 0; width < bounds.Width; width++) { matrix[0, width] = width; };

    for (int height = 1; height < bounds.Height; height++) {
      for (int width = 1; width < bounds.Width; width++) {
        int cost = (s[height - 1] == t[width - 1]) ? 0 : 1;
        int insertion = matrix[height, width - 1] + 1;
        int deletion = matrix[height - 1, width] + 1;
        int substitution = matrix[height - 1, width - 1] + cost;

        int distance = Math.Min(insertion, Math.Min(deletion, substitution));

        if (height > 1 && width > 1 && s[height - 1] == t[width - 2] && s[height - 2] == t[width - 1]) {
          distance = Math.Min(distance, matrix[height - 2, width - 2] + cost);
        }

        matrix[height, width] = distance;
      }
    }

    return matrix[bounds.Height - 1, bounds.Width - 1];
  }


  public static int Distance(string a, string b) {
    if (a == b) return 0;
    float len = Math.Min(a.Length, b.Length);
    double jw = JWDistance(a, b);
    float dl = DLDistance(a, b) / len;
    float xtra = (10 + Math.Abs(a.Length - b.Length)) / (float)Math.Sqrt(len);
    float cont = (a.IndexOf(b) != -1 || b.IndexOf(a) != -1) ? .1f : 1;

    if (a.IndexOf('.') != -1) {
      string[] parts = a.Split('.');
      foreach (string p in parts) {
        if (p.Length < 3) continue;
        if (b.IndexOf(p.ToLowerInvariant()) != -1) cont *= .9f;
      }
    }
    if (b.IndexOf('.') != -1) {
      string[] parts = b.Split('.');
      foreach (string p in parts) {
        if (p.Length < 3) continue;
        if (a.IndexOf(p.ToLowerInvariant()) != -1) cont *= .9f;
      }
    }

    return (int)(1000 * jw * dl * xtra * cont);
  }

}