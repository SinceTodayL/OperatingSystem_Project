import sys
from PyQt5.QtWidgets import QApplication
from ui import PagingUI

if __name__ == "__main__":
    app = QApplication(sys.argv)
    window = PagingUI()
    window.show()
    sys.exit(app.exec_())

