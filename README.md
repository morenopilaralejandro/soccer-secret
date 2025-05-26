# Soccer Secret 0

**Soccer Secret 0** is a touch-based soccer game for Android, crafted with artificial intelligence and built using the Unity game engine. Enjoy fluid touch controls as you face off against the CPU on your mobile device. Character parameters are conveniently managed via Google Sheets for easy tuning and iteration.

---

## Features

- ⚽ **Touch Gameplay**: Intuitive controls optimized for Android touch screens.
- 🤖 **VS CPU Mode**: Compete against an AI-controlled opponent.
- 🗂 **Google Sheets Integration**: Update character parameters in Google Sheets, then import them directly into your game for rapid prototyping and balancing.

---

## Made With AI

- **Code Generation:** GPT-4.1  
- **Image Generation:** DALL·E 3  
- **Music Generation:** [Enzostvs Ai-jukebox](https://huggingface.co/spaces/enzostvs/ai-jukebox)
- **SFX Generation:** *Placeholder*
![Screenshot](Assets/Screenshots/menu1.png)

---

## Changelog

### Version 0.1
- Added touch gameplay
- Added VS CPU game mode
- WIP

---

## Screenshots

Here are some in-game screenshots to give you a taste of the action:

![Gameplay Screenshot](Assets/Screenshots/gameplay1.png)
![Menu Screenshot](Assets/Screenshots/menu1.png)

---

## How to Play

1. **Install the APK** on your Android device. ([APK](https://huggingface.co/spaces/enzostvs/ai-jukebox))
2. **Touch and drag** on the screen to control your player.
3. **Compete against the CPU** to score goals and win matches.

---

## Editing Character Parameters (Development Process)

1. Character parameters are managed using a Google Sheet. ([soccer-secret-0-sheets](https://drive.google.com/drive/folders/1zS2bfB3gCjEZeVrcTZ_IQVh1Bky9dpiW?usp=sharing)).
2. By modifying attributes such as speed, control, or learnable special moves directly within the sheet, the process of editing and balancing stats becomes much more efficient and manageable than traditional manual data entry.
3. The updated data is then exported as a CSV file. 
4. Within the Unity Editor, the Player Importer tool is used to load and apply these parameters to the game.
![Screenshot](Assets/Screenshots/menu1.png)


### Editing Character Parameters (Development Process)

During development, character parameters are managed using a shared Google Sheet. 
Attributes (such as speed, control, or special moves that can be learned) are modified directly in the sheet, making it much easier to edit and balance stats compared to manual editing. 
The updated data is then exported as a CSV file. Within the Unity Editor, the Player Importer tool is used to load and apply these parameters to the game.

Attributes—such as speed, control, or special moves that can be learned—are modified directly in the sheet. This streamlined approach greatly simplifies the process of editing and balancing stats, making it significantly more efficient than manual data entry.

---

## License

This project is licensed under the [GNU General Public License v3.0](https://www.gnu.org/licenses/gpl-3.0.en.html).

---

> *Soccer Secret 0 – You will survive. Dead or alive!*
