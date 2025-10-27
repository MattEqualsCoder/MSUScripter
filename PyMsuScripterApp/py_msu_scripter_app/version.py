import importlib.metadata

_DISTRIBUTION_METADATA = importlib.metadata.metadata('py-msu-scripter-app')


class Version:

    @staticmethod
    def name():
        return _DISTRIBUTION_METADATA['Version']