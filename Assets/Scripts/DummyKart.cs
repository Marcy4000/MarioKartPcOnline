using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyKart : MonoBehaviour
{
    [SerializeField] private Character[] characters;
    public int selectedCharacter { get; private set; }
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    private float _sensitivity;
    private Vector3 _mouseReference;
    private Vector3 _mouseOffset;
    private Vector3 _rotation;
    private bool _isRotating;

    public void SetCharacter(int character)
    {
        selectedCharacter = character;
        skinnedMeshRenderer.material = characters[character].kartMaterial;
        foreach (var model in characters)
        {
            model.model.SetActive(false);
        }
        characters[character].model.SetActive(true);
    }

    void Start()
    {
        _sensitivity = 0.4f;
        _rotation = Vector3.zero;
    }

    void Update()
    {
        if (_isRotating)
        {
            // offset
            _mouseOffset = (Input.mousePosition - _mouseReference);
            // apply rotation
            //_rotation.y = -(_mouseOffset.x + _mouseOffset.y) * _sensitivity;
            _rotation.y = -(_mouseOffset.x) * _sensitivity;
            _rotation.x = -(_mouseOffset.y) * _sensitivity;
            // rotate
            //transform.Rotate(_rotation);
            transform.eulerAngles += _rotation;
            // store mouse
            _mouseReference = Input.mousePosition;
        }
    }

    void OnMouseDown()
    {
        // rotating flag
        _isRotating = true;

        // store mouse
        _mouseReference = Input.mousePosition;
    }

    void OnMouseUp()
    {
        // rotating flag
        _isRotating = false;
    }
}
