"""Source regression checks; these do not execute Unity gameplay.

Run with: python3 -m unittest discover -s Tests -p 'test_*.py' -v
"""
from pathlib import Path
import re
import unittest


class InspectorWarningTests(unittest.TestCase):
    def test_dusk_warning_is_gated_by_midnight_recipe_knowledge(self):
        source = (Path(__file__).resolve().parents[1] /
                  "Assets/Scripts/InspectorPatrol.cs").read_text()
        # Check the actual notification call, not a detached helper or comment.
        pattern = (
            r'Game\.Notify\(\s*Game\.State\.Knows\("midnight"\)\s*\?\s*'
            r'"(?P<known>[^"\n]*)"\s*:\s*"(?P<unknown>[^"\n]*)"\s*,\s*7\s*\)'
        )
        notification = re.search(pattern, source)
        if notification is None:
            self.fail("Dusk warning must branch on known midnight recipe")
        self.assertEqual(notification['unknown'], "Food inspectors are out on patrol.")
        self.assertEqual(notification['known'],
                         "Night falls. Health inspectors are out on patrol. "
                         "Don't get caught carrying Zeeb's sauce.")


if __name__ == "__main__":
    unittest.main()
