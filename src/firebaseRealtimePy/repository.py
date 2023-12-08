from firebase_admin import credentials
from firebase_admin import db
from firebase_admin import initialize_app
from firebase_admin import storage

class Repository:
    def initialize(self, credentials_path: str, database_url: str, storage_bucket: str) -> None:
        cred = credentials.Certificate(credentials_path)
        initialize_app(cred, {
            'databaseURL': database_url,
            'storageBucket': storage_bucket
        })

    def get(self, path: str) -> dict:
        return db.reference(path).get()
    
    def set(self, path: str, value: dict) -> None:
        db.reference(path).set(value)

    def upload(self, path: str, file_path: str) -> None:
        bucket = storage.bucket()
        blob = bucket.blob(path)
        blob.upload_from_filename(file_path)

    def download(self, path: str, file_path: str) -> None:
        bucket = storage.bucket()
        blob = bucket.blob(path)
        blob.download_to_filename(file_path)

    def delete(self, path: str) -> None:
        db.reference(path).delete()