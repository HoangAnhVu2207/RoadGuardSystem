"""Provision the two owner-approved sample routes through PM and Supervisor APIs."""
import math
from urllib.parse import urlencode

from real_flow_api import checked_asset, identifier, operation, read_json, require, upload, valid_etag

REASON = "Owner-approved smoothed drone reference for demo use. Surveyed accuracy is unknown."
PROJECTS = {
    "DKD": ("Duong Kenh Dong", "kenh-dong", "RG-DEMO-KENH-DONG",
            "20e169b3affc1937da532a92f6eb6e3a2ce14329be1990121abea32aef3d838a"),
    "DS2": ("Duong so 2", "duong-so-2", "RG-DEMO-DUONG-SO-2",
            "9674da8b45f2d839451e38b383b4fb49ad57d78ce6718f704362f77b3930e4c3"),
}


def geometry_input(code, route_file, route_hash, coordinates):
    # Local first-order scale at the center of this short road, using WGS84 radii of curvature.
    # The exact affine inverse returns the original longitude/latitude vertices for display.
    longitude = sum(point[0] for point in coordinates) / len(coordinates)
    latitude = sum(point[1] for point in coordinates) / len(coordinates)
    phi = math.radians(latitude)
    semi_major_axis = 6378137.0
    flattening = 1.0 / 298.257223563
    eccentricity_squared = flattening * (2.0 - flattening)
    denominator = 1.0 - eccentricity_squared * math.sin(phi) ** 2
    east = math.radians(1) * semi_major_axis * math.cos(phi) / math.sqrt(denominator)
    north = math.radians(1) * semi_major_axis * (1.0 - eccentricity_squared) / denominator ** 1.5
    vertices = [{"x": (lon - longitude) * east, "y": (lat - latitude) * north}
                for lon, lat in coordinates]
    length = 0.0
    segments = []
    for start, end in zip(vertices, vertices[1:]):
        segments.append({"kind": "LINE", "start": start, "end": end})
        length += math.sqrt((start["x"] - end["x"]) ** 2 + (start["y"] - end["y"]) ** 2)
    # The server checks end <= its independently summed double and allows a 1e-9 relative gap.
    width = [{"fromOffsetMeters": 0, "toOffsetMeters": length - 1e-10, "widthMeters": 3}]
    profile = {
        "code": f"{code}-LOCAL-SAMPLE", "sourceSrid": 0, "datum": "WGS84",
        "projection": "Local affine metric approximation centered on source centerline",
        "axisOrder": "EN", "metresPerUnit": 1, "sampleOnly": True,
        "sourceReference": route_file, "sourceChecksum": route_hash,
        "operation": {"method": "SAMPLE_AFFINE_TO_WGS84", "convention": "Easting northing to longitude latitude",
                      "direction": "NATIVE_TO_WGS84", "sourceReference": route_file,
                      "parameters": [1 / east, 0, longitude, 0, 1 / north, latitude]},
    }
    return profile, segments, width, length


def foundation(api, code, project, files, assets, users):
    name, suffix, source_key, handover_hash = PROJECTS[code]
    pm_id, pm = users["pm-" + suffix]
    supervisor = users["supervisor-a"][1]
    listing, _ = api.request(code + " projects", "GET", "/projects?limit=100", token=supervisor)
    exact = [p for p in listing["items"] if p["projectCode"] == code and p["name"] == name]
    require(len(exact) == 1 and exact[0]["primaryProjectManagerUserId"] == pm_id, code + " project ownership mismatch")
    project_id = identifier(exact[0]["projectId"])
    root = "/projects/" + project_id
    members, _ = api.request(code + " members", "GET", root + "/members?limit=100", token=pm)
    expected = {users[role][0] for role in ["pm-" + suffix, "operator-" + suffix, "crew-" + suffix + "-1",
                                          "crew-" + suffix + "-2"]}
    require(expected <= {m["userId"] for m in members["items"] if m["status"] == "ACTIVE"},
            code + " active memberships missing")
    route_file = project["routeFile"]
    route_hash = files[route_file]["sha256"]
    geometry = read_json(checked_asset(assets, route_file, route_hash))
    require(geometry["geometry"]["type"] == "LineString" and geometry["properties"]["ownerApproved"] is True
            and geometry["properties"]["project"] == name, "Route does not match approved project")
    wgs84 = geometry["geometry"]["coordinates"]
    physical = project["physical"]
    date_from, date_to = project["warranty"]["startsOn"], project["warranty"]["anniversary"]
    pdf = checked_asset(assets, "handover-demo/" + source_key + "-handover-DEMO.pdf", handover_hash)

    def call(step, path, body=None, *, actor=pm, etag=None, statuses=(200, 201)):
        return api.request(code + " " + step, "POST", root + path, body, token=actor,
                           idem=operation(project_id, step), etag=etag, statuses=statuses)

    file_id = upload(api, supervisor, project_id, project_id, pdf, handover_hash, "DOCUMENT", "application/pdf")
    coverage, head = api.request(code + " coverage", "GET", root + "/road-coverage", token=supervisor)
    sources = [s for s in coverage["sources"] if s["kind"] == "HANDOVER"]
    if not sources:
        coverage, _ = call("handover", "/handover-documents",
                           {"fileId": file_id, "documentNo": "DEMO-" + code, "handoverDate": date_from},
                           actor=supervisor, etag=valid_etag(head))
        sources = [s for s in coverage["sources"] if s["kind"] == "HANDOVER"]
    require(len(sources) == 1 and sources[0]["fileId"] == file_id, code + " handover source mismatch")
    handover_id = identifier(sources[0]["id"])
    profile, segments, width, length = geometry_input(code, route_file, route_hash, wgs84)
    profile_response, _ = call("sample-crs", "/crs-profiles", profile)
    system, _ = call("route-system", "/route-systems", {"code": code, "name": project["name"]})
    draft, headers = call("geometry-draft", "/road-geometry-drafts", {
        "sourceKind": "NATIVE_ALIGNMENT", "sourceCrs": 0, "stationOriginMeters": 0, "changeReason": REASON,
        "widthProfile": width, "surveyWidthMeters": physical["roadWidthMeters"], "roadCode": code,
        "roadName": project["name"], "routeSystemId": identifier(system["id"]), "routeKind": "MAIN",
        "tessellationToleranceMeters": 0.10, "nativeAlignment": {
            "crsProfileRevisionId": identifier(profile_response["id"]), "spatialSrid": 0, "primitives": segments,
        },
    })
    draft_id = identifier(draft["id"])
    draft_path = f"/road-geometry-drafts/{draft_id}"
    preview, _ = call("geometry-preview", draft_path + "/preview", statuses=(200,))
    require(preview["sampleOnly"] and abs(preview["lengthMeters"] - length) < 1e-7,
            f"{code} preview lost the sample classification or reference length")
    _, submitted_headers = call("geometry-submit", draft_path + "/submit", etag=valid_etag(headers))
    route, _ = call("geometry-confirm", draft_path + "/confirm", {
        "expectedCurrentVersionId": None, "effectiveFrom": date_from + "T00:00:00+07:00", "reason": REASON,
    }, actor=supervisor, etag=valid_etag(submitted_headers))
    route_id, road_id = identifier(route["routeVersionId"]), identifier(route["roadSectionId"])
    geometry_path = f"/road-sections/{road_id}/versions/{route_id}"
    set_path = geometry_path + "/segment-sets"
    segment_set, headers = call("segment-set", set_path, {"targetLengthMeters": 100, "remainderMode": "KEEP"})
    set_id = identifier(segment_set["id"])
    published, _ = call("segment-publish", f"{set_path}/{set_id}/publish", {
        "expectedPublishedSetId": None, "reason": REASON,
    }, etag=valid_etag(headers))
    require(published["status"] == "PUBLISHED" and len(published["segments"]) > 0,
            f"{code} segment set did not publish")
    segment_ids = [identifier(segment["id"]) for segment in published["segments"]]
    package, package_headers = api.request(f"{code} geometry package", "GET",
                                          root + "/geometry-package?" + urlencode({
                                              "routeVersionId": route_id, "segmentSetId": set_id,
                                          }), token=pm)
    package_route, package_set = package["route"], package["segmentSet"]
    require(package["projectId"] == project_id and package_route["routeVersionId"] == route_id
            and package_route["isCurrent"] and package_route["metadataStatus"] == "COMPLETE"
            and package_route["sampleOnly"], f"{code} current sample route check failed")
    require(package_set["id"] == set_id and package_set["routeVersionId"] == route_id
            and package_set["status"] == "PUBLISHED" and package_set["metadataStatus"] == "COMPLETE"
            and [segment["id"] for segment in package_set["segments"]] == segment_ids,
            f"{code} published segment scope check failed")

    plan, headers = call("pavement-plan", f"/route-versions/{route_id}/segment-sets/{set_id}/pavement-plans", {
        "layout": {"stripWidthMeters": physical["roadWidthMeters"], "slabLengthMeters": physical["slabLengthMeters"],
                   "widthProfile": [{"fromOffsetMeters": 0, "toOffsetMeters": route["lengthMeters"],
                                     "widthMeters": physical["roadWidthMeters"]}],
                   "expansionJointSpacingMeters": physical["expansionJointSpacingMeters"],
                   "expansionJointGapMillimeters": physical["interSlabGapMillimeters"]},
        "displayToleranceMeters": 0.10,
    })
    layout_id = identifier(plan["id"])
    publication, _ = call("sample-map", "/geometry-map-publications", {
        "layoutRevisionId": layout_id, "publicationMode": "SAMPLE", "reason": REASON,
    }, etag=valid_etag(headers))
    publication_id = identifier(publication["publicationId"])
    manifest, _ = api.request(f"{code} sample map read-back", "GET",
                              root + f"/geometry-map-publications/{publication_id}", token=pm)
    require(manifest["sampleOnly"] and manifest["projectId"] == project_id
            and manifest["routeVersionId"] == route_id and manifest["segmentSetId"] == set_id
            and manifest["layoutRevisionId"] == layout_id and manifest["contentHash"] == publication["contentHash"],
            f"{code} sample map manifest mismatch")
    warranty, _ = call("road-warranty", "/warranties", {
        "roadSectionId": road_id, "handoverDocumentId": handover_id,
        "handoverDate": date_from, "warrantyStartDate": date_from, "warrantyEndDate": date_to,
        "scope": "ROAD_SECTION", "sourceDocumentId": file_id, "status": "ACTIVE",
        "operationId": operation(project_id, "road-warranty"),
    }, actor=supervisor)
    require(warranty["roadSectionId"] == road_id and warranty["scope"] == "ROAD_SECTION"
            and warranty["warrantyStartDate"] == date_from and warranty["warrantyEndDate"] == date_to,
            f"{code} warranty did not match the approved road scope")
    return {
        "handoverDocumentId": handover_id, "handoverFileId": file_id,
        "projectId": project_id, "roadSectionId": road_id, "routeVersionId": route_id, "segmentSetId": set_id,
        "segmentIds": segment_ids, "geometryPackageEtag": valid_etag(package_headers),
        "sampleMapPublicationId": publication_id, "pavementLayoutId": layout_id,
        "sampleMapContentHash": manifest["contentHash"],
        "roadWarrantyId": identifier(warranty["warrantyId"]), "geometrySourceSha256": route_hash,
    }
