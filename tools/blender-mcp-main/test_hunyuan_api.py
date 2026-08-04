"""Offline tests for Hunyuan3D request validation and MCP wiring.

These tests never import bpy, read credentials, or send network requests.
"""
import ast
import pathlib

import pytest


ROOT = pathlib.Path(__file__).parent
ADDON = ROOT / "addon.py"
SERVER = ROOT / "src" / "blender_mcp" / "server.py"


def _load_request_builder():
    source = ADDON.read_text(encoding="utf-8")
    tree = ast.parse(source)
    constant_names = {
        "HUNYUAN_RAPID_RESULT_FORMATS",
        "HUNYUAN_PRO_RESULT_FORMATS",
    }
    function_names = {
        "_normalize_hunyuan_api_tier",
        "_build_hunyuan_submit_request",
    }
    selected = []
    for node in tree.body:
        if isinstance(node, ast.Assign):
            assigned_names = {
                target.id for target in node.targets if isinstance(target, ast.Name)
            }
            if assigned_names & constant_names:
                selected.append(node)
        elif isinstance(node, ast.FunctionDef) and node.name in function_names:
            selected.append(node)

    namespace = {}
    module = ast.Module(body=selected, type_ignores=[])
    exec(compile(module, str(ADDON), "exec"), namespace)
    return namespace["_build_hunyuan_submit_request"]


build_request = _load_request_builder()


def test_sources_parse():
    ast.parse(ADDON.read_text(encoding="utf-8"))
    ast.parse(SERVER.read_text(encoding="utf-8"))


def test_rapid_glb_image_request():
    action, data = build_request(
        api_tier="rapid",
        image_fields={"ImageBase64": "abc"},
        result_format="GLB",
        enable_pbr=True,
    )
    assert action == "SubmitHunyuanTo3DRapidJob"
    assert data == {
        "ImageBase64": "abc",
        "EnablePBR": True,
        "ResultFormat": "GLB",
    }


def test_rapid_defaults_to_glb():
    _, data = build_request(
        api_tier="rapid",
        image_fields={"ImageBase64": "abc"},
    )
    assert data["ResultFormat"] == "GLB"


def test_rapid_geometry_omits_pbr():
    action, data = build_request(
        api_tier="rapid",
        text_prompt="白模",
        generate_type="Geometry",
        result_format="GLB",
    )
    assert action == "SubmitHunyuanTo3DRapidJob"
    assert data["EnableGeometry"] is True
    assert "EnablePBR" not in data


def test_rapid_rejects_face_count_before_network():
    with pytest.raises(ValueError, match="FaceCount"):
        build_request(
            api_tier="rapid",
            image_fields={"ImageUrl": "https://example.test/a.png"},
            face_count=100000,
        )


def test_rapid_rejects_prompt_over_200_characters():
    with pytest.raises(ValueError, match="200"):
        build_request(api_tier="rapid", text_prompt="a" * 201)


def test_pro_normal_request_and_default_obj_glb_group():
    action, data = build_request(
        api_tier="pro",
        image_fields={"ImageBase64": "abc"},
        model="3.0",
        face_count=100000,
        generate_type="Normal",
        enable_pbr=True,
        result_format=None,
    )
    assert action == "SubmitHunyuanTo3DProJob"
    assert data == {
        "ImageBase64": "abc",
        "Model": "3.0",
        "GenerateType": "Normal",
        "EnablePBR": True,
        "FaceCount": 100000,
    }
    assert "ResultFormat" not in data


def test_pro_rejects_explicit_glb_result_format():
    with pytest.raises(ValueError, match="omit it"):
        build_request(
            api_tier="pro",
            image_fields={"ImageBase64": "abc"},
            result_format="GLB",
        )


def test_pro_rejects_face_count_out_of_range():
    with pytest.raises(ValueError, match="3000"):
        build_request(
            api_tier="pro",
            image_fields={"ImageBase64": "abc"},
            face_count=2999,
        )


def test_pro_31_rejects_low_poly():
    with pytest.raises(ValueError, match="3.1"):
        build_request(
            api_tier="pro",
            image_fields={"ImageBase64": "abc"},
            model="3.1",
            generate_type="LowPoly",
        )


def test_pro_low_poly_ignores_face_count_and_sets_polygon_type():
    _, data = build_request(
        api_tier="pro",
        image_fields={"ImageBase64": "abc"},
        face_count=80000,
        generate_type="LowPoly",
        polygon_type="quadrilateral",
    )
    assert "FaceCount" not in data
    assert data["PolygonType"] == "quadrilateral"


def test_multiview_requires_primary_image():
    with pytest.raises(ValueError, match="primary"):
        build_request(
            api_tier="pro",
            multiview_payload=[
                {"ViewType": "left", "ViewImageBase64": "abc"}
            ],
        )


def test_server_exposes_new_and_compatibility_tools():
    source = SERVER.read_text(encoding="utf-8")
    assert "def query_hunyuan3d_job(" in source
    assert "def poll_hunyuan_job_status(" in source
    assert '"query_hunyuan_job"' in source
    assert "input_image_path" in source
    assert "multiview_images" in source


def test_addon_contains_new_actions_and_no_legacy_actions():
    source = ADDON.read_text(encoding="utf-8")
    for action in (
        "SubmitHunyuanTo3DRapidJob",
        "QueryHunyuanTo3DRapidJob",
        "SubmitHunyuanTo3DProJob",
        "QueryHunyuanTo3DProJob",
    ):
        assert action in source
    assert '"SubmitHunyuanTo3DJob"' not in source
    assert '"QueryHunyuanTo3DJob"' not in source
    assert '"2023-09-01"' not in source
    assert 'service = "ai3d"' in source
    assert 'service = "hunyuan"' not in source


def test_importer_routes_obj_glb_and_fbx():
    source = ADDON.read_text(encoding="utf-8")
    assert "bpy.ops.wm.obj_import" in source
    assert "bpy.ops.import_scene.gltf" in source
    assert "bpy.ops.import_scene.fbx" in source
    assert "_safe_extract_hunyuan_zip" in source
