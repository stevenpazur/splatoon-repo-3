using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OutfitManager : MonoBehaviour
{
    public List<Button> availableClothings;
    public List<string> clothingNames;
    public List<GameObject> clothingModels;
    public List<Button> availableWeapons;
    public List<string> weaponNames;
    public List<GameObject> weaponModels;
    public List<Button> availableHeadgears;
    public List<string> headgearNames;
    public List<GameObject> headgearModels;
    public List<Button> availableShoes;
    public List<string> shoeNames;
    public List<GameObject> shoeModels;

    [Header("Selection Buttons")]
    public List<Button> selectionButtons;

    [Header("Selected Wear")]
    public GameObject selectedClothing;
    public GameObject selectedWeapon;
    public GameObject selectedHeadgear;
    public GameObject selectedShoes;
}
