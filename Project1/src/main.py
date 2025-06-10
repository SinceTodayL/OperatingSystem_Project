import sys
from PyQt5.QtWidgets import QApplication
from dispatch import Dispatcher
from ui import ElevatorUI

def main():
    app = QApplication(sys.argv)
    dispatcher = Dispatcher()
    _ = ElevatorUI(dispatcher)
    sys.exit(app.exec_())


if __name__ == "__main__":
    main()
