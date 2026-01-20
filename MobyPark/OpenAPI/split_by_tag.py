import yaml
from pathlib import Path
import re

INPUT_FILE = 'openapi.yaml'
OUTPUT_DIR = 'src'


def sanitize_name(name):
    return re.sub(r'[^a-zA-Z0-9_-]', '_', name)


def get_tag_for_path(path_item):
    for method in ['get', 'post', 'put', 'patch', 'delete']:
        if method in path_item and 'tags' in path_item[method]:
            tags = path_item[method]['tags']
            if tags:
                return tags[0]
    return 'General'


def update_refs_to_relative(data, depth=2):
    if isinstance(data, dict):
        for k, v in data.items():
            if k == '$ref' and isinstance(v, str) and v.startswith('#/components/schemas/'):
                schema_name = v.split('/')[-1]
                prefix = '../' * depth
                data[k] = f"{prefix}components/schemas/{schema_name}.yaml"
            else:
                update_refs_to_relative(v, depth)
    elif isinstance(data, list):
        for item in data:
            update_refs_to_relative(item, depth)


def main():
    print(f"Reading {INPUT_FILE}...")
    with open(INPUT_FILE, 'r') as f:
        root_api = yaml.safe_load(f)

    base_path = Path(OUTPUT_DIR)
    schemas_path = base_path / 'components' / 'schemas'
    paths_path = base_path / 'paths'

    schemas_path.mkdir(parents=True, exist_ok=True)
    paths_path.mkdir(parents=True, exist_ok=True)

    if 'components' in root_api and 'schemas' in root_api['components']:
        print("Extracting Schemas...")
        for name, content in root_api['components']['schemas'].items():
            update_refs_to_relative(content, depth=2)

            schema_file = schemas_path / f"{name}.yaml"
            with open(schema_file, 'w') as f:
                yaml.dump(content, f, sort_keys=False)

            root_api['components']['schemas'][name] = {
                '$ref': f"./components/schemas/{name}.yaml"
            }

    if 'paths' in root_api:
        print("Extracting Paths and grouping by Tag...")
        for url, content in root_api['paths'].items():
            tag = get_tag_for_path(content)
            tag_folder = paths_path / tag
            tag_folder.mkdir(exist_ok=True)

            filename = sanitize_name(url.strip('/')) + '.yaml'
            file_path = tag_folder / filename

            update_refs_to_relative(content, depth=2)

            with open(file_path, 'w') as f:
                yaml.dump(content, f, sort_keys=False)

            root_api['paths'][url] = {
                '$ref': f"./paths/{tag}/{filename}"
            }

    with open(base_path / 'openapi.yaml', 'w') as f:
        yaml.dump(root_api, f, sort_keys=False)

    print(f"Success! Your organized API is in the '{OUTPUT_DIR}' folder.")


if __name__ == '__main__':
    main()